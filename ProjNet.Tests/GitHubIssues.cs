using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using GeoAPI.CoordinateSystems;
using NUnit.Framework;
using ProjNet.Converters.WellKnownText;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Projections;
using ProjNet.CoordinateSystems.Transformations;
using GeoPosition = GeoAPI.Geometries.Coordinate;
using UtmPosition = GeoAPI.Geometries.Coordinate;

namespace ProjNet.UnitTests
{
    [TestFixture]
    public class GitHubIssues
    {
        private static GeoPosition UtmToLatLon(double x, double y, int zone, bool isNorthernHemisphere)
        {
            IProjectedCoordinateSystem utmCoordSystem = ProjectedCoordinateSystem.WGS84_UTM(zone, isNorthernHemisphere);
            IGeographicCoordinateSystem wgs84 = GeographicCoordinateSystem.WGS84;
            CoordinateTransformationFactory ctFact = new CoordinateTransformationFactory();

            var transform = ctFact.CreateFromCoordinateSystems(utmCoordSystem, wgs84);
            var latLon = transform.MathTransform.Transform(new[] { x, y });

            return new GeoPosition(latLon[1], latLon[0]);
        }

        private static UtmPosition LatLonToUtm(double latitude, double longitude)
        {
            var zone = (int)MapProjection.CalcUtmZone(longitude);
            var isNorthernHemisphere = latitude >= 0;

            IProjectedCoordinateSystem utmCoordSystem = ProjectedCoordinateSystem.WGS84_UTM(zone, isNorthernHemisphere);

            IGeographicCoordinateSystem wgs84 = GeographicCoordinateSystem.WGS84;
            CoordinateTransformationFactory ctFact = new CoordinateTransformationFactory();

            var transform = ctFact.CreateFromCoordinateSystems(wgs84, utmCoordSystem);
            var lonLat = transform.MathTransform.Transform(new[] { longitude, latitude });

            //var latZone = UtmPosition.GetUtmLatitudeZoneLetter(latitude);

            return new UtmPosition(lonLat[0], lonLat[1], zone);
        }

        [Test, Description("UTM accuracy"), Ignore("Input coordinates are not valid")]
        public void TestIssue13()
        {
            var k1 = UtmToLatLon(2000, 2000, 18, true);
            Assert.AreEqual(-79.47082590, k1.Y, 1.0e-6);
            Assert.AreEqual(0.01803920, k1.X, 1.0e-6);

            var k2 = LatLonToUtm(k1.X, k1.Y);
            Assert.That(k2.Z, Is.EqualTo(18d));
        }

        [Test, Description("Help: Israeli Transverse Mercator")]
        public void TestIssue16()
        {
            var coordinateTransformFactory = new CoordinateTransformationFactory();
            var coordinateSystemFactory = new CoordinateSystemFactory();
            var parameters = new List<ProjectionParameter>
            {
                new ProjectionParameter("latitude_of_origin", 31.734393611111109123611111111111),
                new ProjectionParameter("central_meridian", 35.204516944444442572222222222222),
                new ProjectionParameter("false_northing", 626907.390),
                new ProjectionParameter("false_easting", 219529.584),
                new ProjectionParameter("scale_factor", 1.0000067)
            };

            var datum = coordinateSystemFactory.CreateHorizontalDatum("Isreal 1993", DatumType.HD_Geocentric,
                Ellipsoid.GRS80, new Wgs84ConversionInfo(-48, 55, 52, 0, 0, 0, 0));

            var itmGeo = coordinateSystemFactory.CreateGeographicCoordinateSystem("ITM", AngularUnit.Degrees, datum /*HorizontalDatum.ETRF89*/, // GRS80
                PrimeMeridian.Greenwich, new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North));

            var projection = coordinateSystemFactory.CreateProjection("Transverse_Mercator", "Transverse_Mercator", parameters);
            var itm = coordinateSystemFactory.CreateProjectedCoordinateSystem("ITM", itmGeo, projection, LinearUnit.Metre,
                new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North));

            Console.WriteLine(itm.ToString());
            var itm2 =
                coordinateSystemFactory.CreateFromWkt(
                    "PROJCS[\"Israel / Israeli TM Grid\",GEOGCS[\"Israel\",DATUM[\"Israel\",SPHEROID[\"GRS 1980\",6378137,298.257222101,AUTHORITY[\"EPSG\",\"7019\"]],TOWGS84[-48,55,52,0,0,0,0],AUTHORITY[\"EPSG\",\"6141\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4141\"]],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",31.73439361111111],PARAMETER[\"central_meridian\",35.20451694444445],PARAMETER[\"scale_factor\",1.0000067],PARAMETER[\"false_easting\",219529.584],PARAMETER[\"false_northing\",626907.39],AUTHORITY[\"EPSG\",\"2039\"],AXIS[\"Easting\",EAST],AXIS[\"Northing\",NORTH]]");
            Console.WriteLine();
            Console.WriteLine(itm2.ToString());

            string wkt = "GEOGCS[\"GCS_WGS_1984\",DATUM[\"D_WGS_1984\",SPHEROID[\"WGS_1984\",6378137,298.257223563]],PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.0174532925199433]]";
            var wgs84 = CoordinateSystemWktReader.Parse(wkt, Encoding.UTF8) as ICoordinateSystem;

            var jerusalem = new GeoPosition(35.234383488170444, 31.776747919252124);

            var transform = coordinateTransformFactory.CreateFromCoordinateSystems(wgs84, itm);
            var transform2 = coordinateTransformFactory.CreateFromCoordinateSystems(wgs84, itm2);
            var transformed = transform.MathTransform.Transform(jerusalem);
            var transformed2 = transform2.MathTransform.Transform(jerusalem);

            Assert.AreEqual(222286, transformed.X, 0.01);
            Assert.AreEqual(631556, transformed.Y, 0.01);

            Assert.AreEqual(222286, transformed2.X, 0.01);
            Assert.AreEqual(631556, transformed2.Y, 0.01);

            Console.WriteLine(transformed.ToString());
            Console.WriteLine(transformed2.ToString());
        }
    }
}