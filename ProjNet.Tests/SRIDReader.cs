using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Reflection;
using GeoAPI.CoordinateSystems;
using NUnit.Framework;
using ProjNet.Converters.WellKnownText;
using ProjNet.CoordinateSystems;

namespace ProjNet.UnitTests
{
    internal class SRIDReader
    {
        private static readonly string Filename = TestContext.CurrentContext.TestDirectory + @"\SRID.csv";

        public struct WktString {
            /// <summary>
            /// Well-known ID
            /// </summary>
            public int WktId;
            /// <summary>
            /// Well-known Text
            /// </summary>
            public string Wkt;
        }




#if (!PCL)
        public static IEnumerable<WktString> GetSrids(string filename = "")
        {
            if (string.IsNullOrWhiteSpace(filename))
                filename = Filename;

            using (var sr = System.IO.File.OpenText(filename))
            {
                foreach (var sridWkt in GetSrids(sr))
                    yield return sridWkt;
            }
        }
#endif

        /// <summary>
        /// Enumerates all SRID's in the SRID.csv file.
        /// </summary>
        /// <returns>Enumerator</returns>
        public static IEnumerable<WktString> GetSrids(StreamReader sr)
        {
            if (sr == null)
                throw new ArgumentNullException("sr");
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                if (string.IsNullOrEmpty(line)) continue;

                var split = line.IndexOf(';');
                if (split <= -1) continue;

                var wkt = new WktString
                                { 
                                    WktId = int.Parse(line.Substring(0, split)), 
                                    Wkt = line.Substring(split + 1)
                                };
                yield return wkt;
            }
        }

        /// <summary>
        /// Gets a coordinate system from the SRID.csv file
        /// </summary>
        /// <param name="id">EPSG ID</param>
        /// <returns>Coordinate system, or null if SRID was not found.</returns>
        public static ICoordinateSystem GetCSbyID(int id)
        {
            ICoordinateSystemFactory factory = new CoordinateSystemFactory();
#if PCL
            var mrs = typeof(SRIDReader).GetTypeInfo().Assembly.GetManifestResourceStream("ProjNet.UnitTests.SRID.csv");
            using(var sr = new StreamReader(mrs))
            foreach (SRIDReader.WktString wkt in SRIDReader.GetSrids(sr))
#else
            foreach (SRIDReader.WktString wkt in SRIDReader.GetSrids((string) null))
#endif
            { 
                if (wkt.WktId == id)
                    return factory.CreateFromWkt(wkt.Wkt);
            }
            return null;
        }
    }
}
