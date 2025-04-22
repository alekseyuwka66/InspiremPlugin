using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace Inspirem
{
    [Transaction(TransactionMode.Manual)]
    public class LightFixtureAnnotationCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            while (true)
            {
                try
                {
                    Reference spaceRef = uiDoc.Selection.PickObject(
                        ObjectType.Element,
                        new SpaceSelectionFilter(),
                        "Выберите помещение/пространство (ESC для выхода)");

                    SpatialElement selectedSpace = doc.GetElement(spaceRef) as SpatialElement;
                    LightFixtureAnnotation.LightFixtureAnnotation.CreateAnnotation(selectedSpace, doc, uiDoc);
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    return Result.Succeeded;
                }
            }
        }
    }
    [Transaction(TransactionMode.Manual)]
    public class AllLightFixtureAnnotationCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            new FilteredElementCollector(doc, doc.ActiveView.Id)
                .OfClass(typeof(SpatialElement))
                .Cast<SpatialElement>()
                .Where(space => space != null)
                .ToList()
                .ForEach(space => LightFixtureAnnotation.LightFixtureAnnotation.CreateAnnotation(space, doc, uiDoc));

            return Result.Succeeded;
        }
    }
    public class CreateLightingDimensionsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            return Result.Succeeded;
        }
    }

}