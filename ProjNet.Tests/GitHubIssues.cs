using System;
using System.Diagnostics;
using GeoAPI.CoordinateSystems;
using NUnit.Framework;
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

    }
}