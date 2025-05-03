using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using pluginspirem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

public class LightFixtureDimensions
{
    public static void CreateDimensions(SpatialElement selectedSpace, Document doc, UIDocument uiDoc)
    {
        var lightFixtures = LightFixture.GetFixtures(selectedSpace, doc);
        var dimensions = CreateDimensions(lightFixtures, doc);
    }

    private static List<Dimension> CreateDimensions(List<Element> lightFixtures, Document doc)
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
                    Height = LightFixture.GetFixtureHeight(f)
                })
                .ToList();

            foreach (var group in groupedFixtures)
            {
                try
                {
                    CreateAxisDimensions(group, CoordAxis.X, dimType, doc);
                    CreateAxisDimensions(group, CoordAxis.Y, dimType, doc);

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


    private static void CreateAxisDimensions(
        IGrouping<dynamic, FamilyInstance> group,
        CoordAxis axis,
        DimensionType dimType,
        Document doc)
    {
        var grouped = group
            .GroupBy(f => Math.Round(GetCoord(f.Location as LocationPoint, axis), 3))
            .Where(g => g.Count() > 1);

        foreach (var g in grouped)
        {
            var ordered = g.OrderBy(f => GetCoord(f.Location as LocationPoint, OppositeAxis(axis))).ToList();

            ordered.Zip(ordered.Skip(1), (current, next) => CreateDimensionBetweenFixtures(current, next, axis, dimType, doc)).ToList();

        }
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
