using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using pluginspirem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

public class LightFixtureDimensions
{
    public static void CreateDimensions(SpatialElement selectedSpace, Document doc)
    {
        var lightFixtures = LightFixture.GetFixtures(selectedSpace, doc);
        var dimensions = CreateDimensions(lightFixtures, selectedSpace, doc);
    }

    private static List<Dimension> CreateDimensions(List<Element> lightFixtures, SpatialElement selectedSpace, Document doc)
    {
        var dimensions = new List<Dimension>();

        using (Transaction trans = new Transaction(doc, "Create Fixture Dimensions"))
        {
            trans.Start();

            var dimType = GetDimensionType(doc);

            var groupedFixtures = lightFixtures
                .OfType<FamilyInstance>()
                .GroupBy(f => new
                {
                    TypeName = f.Symbol.Id,
                })
                .ToList();

            foreach (var group in groupedFixtures)
            {
                try
                {
                    foreach (var axis in new[] { CoordAxis.X, CoordAxis.Y })
                    {
                        CreateFixtureDimensions(group, axis, dimType, doc);
                    }
                    CreateWallDimensions(group, selectedSpace, dimType, doc);
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Ошибка создания размеров", ex.Message);
                }
            }

            trans.Commit();
        }
        return dimensions;
    }
    private static void CreateWallDimensions(
    IGrouping<dynamic, FamilyInstance> group,
    SpatialElement space,
    DimensionType dimType,
    Document doc)
    {
        var edgeFixtures = new List<FamilyInstance>();

        foreach (var axis in new[] { CoordAxis.X, CoordAxis.Y })
        {
            FamilyInstance minFixture = null;
            FamilyInstance maxFixture = null;

            if (axis == CoordAxis.X)
            {
                minFixture = group
                    .Select(f => new { F = f, L = f.Location as LocationPoint })
                    .OrderBy(p => Math.Round(p.L.Point.X, 3))
                    .ThenByDescending(p => Math.Round(p.L.Point.Y, 3))
                    .Select(p => p.F)
                    .FirstOrDefault();
                edgeFixtures.Add(minFixture);

                maxFixture = group
                    .Select(f => new { F = f, L = f.Location as LocationPoint })
                    .OrderByDescending(p => Math.Round(p.L.Point.X, 3))
                    .ThenBy(p => Math.Round(p.L.Point.Y, 3))
                    .Select(p => p.F)
                    .FirstOrDefault();
                edgeFixtures.Add(maxFixture);
            }
            else
            {
                minFixture = group
                    .Select(f => new { F = f, L = f.Location as LocationPoint })
                    .OrderBy(p => Math.Round(p.L.Point.Y, 3))
                    .ThenBy(p => Math.Round(p.L.Point.X, 3))
                    .Select(p => p.F)
                    .FirstOrDefault();
                edgeFixtures.Add(minFixture);

                maxFixture = group
                    .Select(f => new { F = f, L = f.Location as LocationPoint })
                    .OrderByDescending(p => Math.Round(p.L.Point.Y, 3))
                    .ThenByDescending(p => Math.Round(p.L.Point.X, 3))
                    .Select(p => p.F)
                    .FirstOrDefault();
                edgeFixtures.Add(maxFixture);
            }
        }
        CreateDimensionsBetweenFixtureAndWalls(edgeFixtures, space, dimType, doc);
    }
    private static void CreateDimensionsBetweenFixtureAndWalls(
    List<FamilyInstance> edgeFixtures, // Порядок: (X.min,Y.max), (X.max,Y.max), (X.min,Y.min), (X.max,Y.min)
    SpatialElement space,
    DimensionType dimType,
    Document doc)
    {
        foreach (var axis in new[] { CoordAxis.X, CoordAxis.Y })
        {
            var refType = axis == CoordAxis.X
                ? FamilyInstanceReferenceType.CenterLeftRight
                : FamilyInstanceReferenceType.CenterFrontBack;

            var wallRefs = GetWallsReference(
                    edgeFixtures,
                    axis,
                    doc,
                    out List<XYZ> intersectionPoints);

            var dimensions = new List<Dimension>();

            for (int i = 0; i < 4; i++)
            {
                var fixture = edgeFixtures[i];
                var loc = fixture.Location as LocationPoint;
                var fixtureRef = fixture.GetReferences(refType).First();

                var wallRef = wallRefs[i];

                var intersectionPoint = intersectionPoints[i];


                var dimensionLine = CreateDimensionLine(loc.Point, intersectionPoint, axis);

                var refArray = new ReferenceArray();
                refArray.Append(fixtureRef);
                refArray.Append(wallRef);

                var dimension = doc.Create.NewDimension(doc.ActiveView, dimensionLine, refArray, dimType);

                dimensions.Add(dimension);
            }
            if (axis == CoordAxis.X)
            {
                if (dimensions[0].Value == dimensions[2].Value)
                {
                    var d = dimensions[2];
                    doc.Delete(d.Id);
                }
                if (dimensions[1].Value == dimensions[3].Value)
                {
                    var d = dimensions[1];
                    doc.Delete(d.Id);
                }
            }
            else
            {
                if (dimensions[1].Value == dimensions[2].Value)
                {
                    var d = dimensions[1];
                    doc.Delete(d.Id);
                }
                if (dimensions[0].Value == dimensions[3].Value)
                {
                    var d = dimensions[3];
                    doc.Delete(d.Id);
                }
            }
        }
    }

    private static List<Reference> GetWallsReference(List<FamilyInstance> edgeFixtures, CoordAxis axis, Document doc, out List<XYZ> intersectionPoints)
    {
        XYZ direction = (axis == CoordAxis.X) ? XYZ.BasisX : XYZ.BasisY;

        var locPoints = edgeFixtures.Select(fi => (fi.Location as LocationPoint)?.Point).ToList();

        var rays = new List<Line>();

        if (axis == CoordAxis.X)
        {
            rays.Add(Line.CreateBound(locPoints[0], locPoints[0] - direction * 100));
            rays.Add(Line.CreateBound(locPoints[1], locPoints[1] + direction * 100));
            rays.Add(Line.CreateBound(locPoints[2], locPoints[2] - direction * 100));
            rays.Add(Line.CreateBound(locPoints[3], locPoints[3] + direction * 100));
        }
        else
        {
            rays.Add(Line.CreateBound(locPoints[0], locPoints[0] + direction * 100));
            rays.Add(Line.CreateBound(locPoints[1], locPoints[1] - direction * 100));
            rays.Add(Line.CreateBound(locPoints[2], locPoints[2] - direction * 100));
            rays.Add(Line.CreateBound(locPoints[3], locPoints[3] + direction * 100));
        }

        ElementCategoryFilter wallFilter = new ElementCategoryFilter(BuiltInCategory.OST_Walls);
        var referenceIntersector = new ReferenceIntersector(
            wallFilter,
            FindReferenceTarget.Face,
            Get3DView(doc));

        var references = new List<Reference>();

        intersectionPoints = new List<XYZ>();

        foreach (var ray in rays)
        {
            ReferenceWithContext referenceWithContext =
                referenceIntersector.FindNearest(ray.GetEndPoint(0), ray.Direction);

            if (referenceWithContext != null)
            {
                XYZ intersectionPoint = ray.GetEndPoint(0) + ray.Direction * referenceWithContext.Proximity;
                Reference reference = referenceWithContext.GetReference();

                Element wall = doc.GetElement(reference.ElementId);
                if (wall is Wall)
                {
                    references.Add(reference);
                    intersectionPoints.Add(intersectionPoint);
                }

            }
        }

        return references;
    }


    private static View3D Get3DView(Document doc)
    {
        FilteredElementCollector collector
          = new FilteredElementCollector(doc);

        collector.OfClass(typeof(View3D));

        foreach (View3D v in collector)
        {

            if (v != null && !v.IsTemplate && v.Name == "{3D}")
            {
                return v;
            }
        }
        return null;
    }


    private static void CreateFixtureDimensions(
    IGrouping<dynamic, FamilyInstance> group,
    CoordAxis axis,
    DimensionType dimType,
    Document doc)
    {
        if (axis == CoordAxis.Y)
        {
            CreateRowDimensions(group, dimType, doc);
        }
        else
        {
            CreateInterRowDimension(group, dimType, doc);
        }
    }

    private static void CreateInterRowDimension(
    IGrouping<dynamic, FamilyInstance> group,
    DimensionType dimType,
    Document doc)
    {
        var rows = group
            .GroupBy(f => Math.Round(GetCoord(f.Location as LocationPoint, CoordAxis.Y), 3))
            .OrderBy(g => g.Key)
            .ToList();

        if (rows.Count < 2) return;

        for (int i = 0; i < rows.Count - 1; i++)
        {
            var row1 = rows[i].OrderBy(f => GetCoord(f.Location as LocationPoint, CoordAxis.X)).ToList();
            var row2 = rows[i + 1].OrderBy(f => GetCoord(f.Location as LocationPoint, CoordAxis.X)).ToList();

            var fi1 = row1.FirstOrDefault();
            var fi2 = row2.FirstOrDefault();

            if (fi1 != null && fi2 != null)
            {
                CreateDimensionBetweenFixtures(fi1, fi2, CoordAxis.X, dimType, doc);
            }
        }
    }

    private static void CreateRowDimensions(
        IGrouping<dynamic, FamilyInstance> group,
        DimensionType dimType,
        Document doc)
    {
        var groupedByRow = group
            .GroupBy(f => Math.Round(GetCoord(f.Location as LocationPoint, CoordAxis.Y), 3))
            .Where(g => g.Count() > 1);
        var row = groupedByRow.First();
        //foreach (var row in groupedByRow)
        var ordered = row.OrderBy(f => GetCoord(f.Location as LocationPoint, CoordAxis.X)).ToList();

            ordered.Zip(ordered.Skip(1), (current, next) =>
                CreateDimensionBetweenFixtures(current, next, CoordAxis.Y, dimType, doc)).ToList();
    }


    private static Dimension CreateDimensionBetweenFixtures(
        FamilyInstance fi1,
        FamilyInstance fi2,
        CoordAxis axis,
        DimensionType dimType,
        Document doc)
    {
        if (fi1.LevelId != fi2.LevelId) return null;

        var refType = axis == CoordAxis.X
            ? FamilyInstanceReferenceType.CenterFrontBack
            : FamilyInstanceReferenceType.CenterLeftRight;


            var refArray = new ReferenceArray();
            refArray.Append(fi1.GetReferences(refType).First());
            refArray.Append(fi2.GetReferences(refType).First());

            var line = CreateDimensionLine(fi1.Location as LocationPoint,
                                        fi2.Location as LocationPoint,
                                        axis);

            return doc.Create.NewDimension(doc.ActiveView, line, refArray, dimType);
    }

    private static Line CreateDimensionLine(LocationPoint loc1, LocationPoint loc2, CoordAxis axis)
    {
        var p1 = loc1.Point;
        var p2 = loc2.Point;

        var mid = (p1 + p2) / 2;

        return axis == CoordAxis.X
            ? Line.CreateBound(mid + XYZ.BasisY * 2, mid - XYZ.BasisY * 2)
            : Line.CreateBound(mid + XYZ.BasisX * 2, mid - XYZ.BasisX * 2);
    }
    private static Line CreateDimensionLine(XYZ p1, XYZ p2, CoordAxis axis)
    {
        var mid = (p1 + p2) / 2;

        return axis == CoordAxis.Y
            ? Line.CreateBound(mid + XYZ.BasisY * 2, mid - XYZ.BasisY * 2)
            : Line.CreateBound(mid + XYZ.BasisX * 2, mid - XYZ.BasisX * 2);
    }

    private enum CoordAxis { X, Y }
    private static double GetCoord(LocationPoint loc, CoordAxis axis)
    {
        return axis == CoordAxis.X ? loc.Point.X : loc.Point.Y;
    }
    private static CoordAxis OppositeAxis(CoordAxis axis)
    {
        return axis == CoordAxis.X ? CoordAxis.Y : CoordAxis.X;
    }
    private static DimensionType GetDimensionType(Document doc, string typeName = "ISOCPEUR Dimension")
    {
        DimensionType dimensionType = new FilteredElementCollector(doc)
            .OfClass(typeof(DimensionType))
            .Cast<DimensionType>()
            .FirstOrDefault(dt => dt.Name == typeName);

        if (dimensionType == null)
        {
            DimensionType defaultType = new FilteredElementCollector(doc)
                .OfClass(typeof(DimensionType))
                .Cast<DimensionType>()
                .First();

            dimensionType = defaultType.Duplicate(typeName) as DimensionType;

            // Существующие настройки
            dimensionType.get_Parameter(BuiltInParameter.TEXT_FONT).Set("ISOCPEUR");
            dimensionType.get_Parameter(BuiltInParameter.TEXT_SIZE).Set(2.5 / 304.8);
            dimensionType.get_Parameter(BuiltInParameter.DIM_TEXT_BACKGROUND).Set(1);
            dimensionType.LookupParameter("Вес линий").Set(1);
            dimensionType.LookupParameter("Тип выноски").Set(1);
            dimensionType.LookupParameter("Отступ текста").Set(0.5 / 304.8);
            dimensionType.LookupParameter("Показать высоту проема").Set(0);
            dimensionType.LookupParameter("Жирный").Set(0);

            // Новые параметры графики
            //dimensionType.LookupParameter("Длина полки").Set(1.0 / 304.8); // 1 мм в футах
            //dimensionType.LookupParameter("Показать выноску за при перемещение текста").Set(1);
            //dimensionType.LookupParameter("Вес линии засечек").Set(5);

            dimensionType.LookupParameter("Удлинение размерной линии").Set(0.0);

            //dimensionType.LookupParameter("Развернутое удлинение размерной линии").Set(3.175 / 304.8);

            dimensionType.LookupParameter("Удлинение вспомогательной линии").Set(2.0 / 304.8);

            // Настройка формата единиц (1230 мм)
            FormatOptions formatOptions = dimensionType.GetUnitsFormatOptions();
            //formatOptions.SetUnitTypeId(UnitTypeId.Millimeters);
            //formatOptions.SuppressSpaces = true;
            //formatOptions.Accuracy = 1;
            //formatOptions.UseDigitGrouping = false;
            dimensionType.SetUnitsFormatOptions(formatOptions);

        }

        return dimensionType;
    }
}
