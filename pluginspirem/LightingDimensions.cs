//using Autodesk.Revit.DB;
//using Autodesk.Revit.UI;
//using Autodesk.Revit.Attributes;
//using System.Collections.Generic;
//using System.Linq;
//using System;

//public class LightingDimensions
//{
//    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
//    {
//        UIDocument uiDoc = commandData.Application.ActiveUIDocument;
//        Document doc = uiDoc.Document;

//        // Получаем выбранные светильники
//        var selectedLights = uiDoc.Selection.GetElementIds()
//            .Select(id => doc.GetElement(id))
//            .OfType<FamilyInstance>()
//            .Where(fi => fi.Category?.Id.IntegerValue == (int)BuiltInCategory.OST_LightingFixtures)
//            .ToList();

//        if (selectedLights.Count < 2)
//        {
//            TaskDialog.Show("Ошибка", "Выберите как минимум 2 светильника");
//            return Result.Failed;
//        }

//        // Получаем стиль размера из проекта (или создаем новый)
//        DimensionType dimensionType = GetDimensionStyle(doc, "Стиль для светильников");

//        // Сортируем светильники по координатам
//        var orderedLights = selectedLights
//            .OrderBy(l => (l.Location as LocationPoint)?.Point.X)
//            .ThenBy(l => (l.Location as LocationPoint)?.Point.Y)
//            .ToList();

//        using (Transaction trans = new Transaction(doc, "Создание привязок светильников"))
//        {
//            trans.Start();

//            // Создаем размеры между соседними светильниками
//            for (int i = 0; i < orderedLights.Count - 1; i++)
//            {
//                CreateDimensionBetweenFixtures(
//                    doc,
//                    doc.ActiveView,
//                    orderedLights[i],
//                    orderedLights[i + 1],
//                    dimensionType);
//            }

//            // Дополнительно: создаем общий размер между крайними светильниками
//            if (orderedLights.Count > 2)
//            {
//                CreateDimensionBetweenFixtures(
//                    doc,
//                    doc.ActiveView,
//                    orderedLights.First(),
//                    orderedLights.Last(),
//                    dimensionType);
//            }

//            trans.Commit();
//        }

//        TaskDialog.Show("Готово", $"Создано {selectedLights.Count} привязок между светильниками");
//        return Result.Succeeded;
//    }

//    private void CreateDimensionBetweenFixtures(
//        Document doc,
//        View view,
//        FamilyInstance fixture1,
//        FamilyInstance fixture2,
//        DimensionType dimensionType)
//    {
//        if (!(fixture1.Location is LocationPoint loc1) || !(fixture2.Location is LocationPoint loc2))
//            return;

//        // Определяем направление размера (горизонтальное/вертикальное)
//        bool isHorizontal = Math.Abs(loc1.Point.X - loc2.Point.X) > Math.Abs(loc1.Point.Y - loc2.Point.Y);
//        XYZ direction = isHorizontal ? XYZ.BasisY : XYZ.BasisX;

//        // Создаем References для размера
//        ReferenceArray refArray = new ReferenceArray();
//        refArray.Append(new Reference(fixture1));
//        refArray.Append(new Reference(fixture2));

//        // Вычисляем позицию линии размера (середина между светильниками + отступ)
//        XYZ midPoint = (loc1.Point + loc2.Point) / 2;
//        double offset = 2.0; // Отступ в единицах проекта
//        XYZ lineStart = midPoint + direction * offset;
//        XYZ lineEnd = midPoint - direction * offset;

//        // Создаем линию размера
//        Line dimensionLine = Line.CreateBound(lineStart, lineEnd);

//        // Создаем размер
//        Dimension dimension = doc.Create.NewDimension(view, dimensionLine, refArray);

//        // Применяем выбранный стиль
//        if (dimensionType != null)
//        {
//            dimension.DimensionType = dimensionType;
//        }
//    }

//    private DimensionType GetDimensionStyle(Document doc, string styleName)
//    {
//        // Пытаемся найти существующий стиль
//        DimensionType existingType = new FilteredElementCollector(doc)
//            .OfClass(typeof(DimensionType))
//            .Cast<DimensionType>()
//            .FirstOrDefault(dt => dt.Name == styleName);

//        if (existingType != null)
//            return existingType;

//        // Создаем новый стиль на основе стандартного
//        DimensionType defaultType = new FilteredElementCollector(doc)
//            .OfClass(typeof(DimensionType))
//            .Cast<DimensionType>()
//            .FirstOrDefault(dt => dt.Name == "Linear Dimension Style");

//        if (defaultType == null)
//            return null;

//        using (Transaction trans = new Transaction(doc, "Create Dimension Style"))
//        {
//            trans.Start();

//            DimensionType newType = defaultType.Duplicate(styleName) as DimensionType;

//            // Настраиваем стиль (пример настроек)
//            newType.get_Parameter(BuiltInParameter.LINE_PEN).Set(5); // Толщина линии
//            //newType.get_Parameter(BuiltInParameter.DIM_TEXT_SIZE).Set(0.02); // Размер текста
//            //newType.get_Parameter(BuiltInParameter.DIM_TEXT_FONT).Set("Arial");

//            trans.Commit();
//            return newType;
//        }
//    }
//}