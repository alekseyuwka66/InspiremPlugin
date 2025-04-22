using Autodesk.Revit.UI.Events;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Inspirem

{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    internal class OnStartSettings : IExternalApplication
    {
        public Result OnShutdown(UIControlledApplication application)
        {
            try
            {

            }
            catch { return Result.Failed; }
            return Result.Succeeded;
        }
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                RibbonCreator.OnStartup(application);
            }
            catch { return Result.Failed; }
            return Result.Succeeded;
        }

    }
}
