using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginspirem
{
    static class LightFixture
    {
        public static List<Element> GetFixtures(SpatialElement selectedSpace, Document doc)
        {
            Solid roomSolid = null;
            View activeView = doc.ActiveView;

            try
            {
                SpatialElementGeometryCalculator calculator = new SpatialElementGeometryCalculator(doc);
                SpatialElementGeometryResults results = calculator.CalculateSpatialElementGeometry(selectedSpace);

                roomSolid = results.GetGeometry();
            }
            catch (Exception ex)
            {

                throw ex = new Exception($"Не удалось получить геометрию помещения. ElementId - {selectedSpace.Id}");
            }

            FilteredElementCollector collector = new FilteredElementCollector(doc, activeView.Id)
                .OfCategory(BuiltInCategory.OST_LightingFixtures)
                .WhereElementIsNotElementType();


            var fixturesInRoom = collector
                .Select(fixture => new
                {
                    Element = fixture,
                    Location = fixture.Location as LocationPoint
                })
                .Where(x => x.Location != null && IsPointInsideSolid(roomSolid, x.Location.Point))
                .Select(x => x.Element)
                .ToList();

            return fixturesInRoom;
        }
        private static bool IsPointInsideSolid(Solid solid, XYZ point)
        {
            XYZ direction = XYZ.BasisX;
            double rayLength = 1000;

            Line ray = Line.CreateBound(point, point + direction.Multiply(rayLength));
            int intersections = 0;

            foreach (Face face in solid.Faces)
            {
                IntersectionResultArray results;
                var result = face.Intersect(ray, out results);

                if (result == SetComparisonResult.Overlap && results != null)
                {
                    intersections += results.Size;
                }
            }

            return intersections % 2 == 1;
        }
        public static double GetFixtureHeight(Element fixture)
        {
            Parameter elevationParam = fixture.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM);
            if (elevationParam != null && elevationParam.HasValue)
            {
                return UnitUtils.ConvertFromInternalUnits(elevationParam.AsDouble(), UnitTypeId.Millimeters);
            }
            return 0;
        }
        public static string GetFixturePower(Element fixture)
        {
            if (fixture is FamilyInstance familyInstance)
            {
                ElementType type = familyInstance.Document.GetElement(familyInstance.GetTypeId()) as ElementType;
                if (type != null)
                {
                    Parameter powerParam = type.LookupParameter("ADSK_Номинальная мощность");
                    if (powerParam != null && powerParam.HasValue)
                    {
                        return powerParam.AsValueString();
                    }
                }
            }
            return "0 Вт";
        }
        public static XYZ GetLocationPoint(Element element)
        {
            return (element?.Location as LocationPoint)?.Point;
        }
    }
}
