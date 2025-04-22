using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace Inspirem
{
    public static class RibbonCreator
    {
        private static string thisAssemblyPath
        {
            get { return Assembly.GetExecutingAssembly().Location; }
        }

        public static Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Create ribbon tab
                string tabName = "Inspirem";
                application.CreateRibbonTab(tabName);

                // Create panel on the tab
                RibbonPanel panel = application.CreateRibbonPanel(tabName, "Аннотации светильников");

                // Create a pull-down button to group related commands
                PulldownButtonData pulldownData = new PulldownButtonData(
                    "LightFixtureTools",
                    "Аннотации светильников");

                PulldownButton pulldownButton = panel.AddItem(pulldownData) as PulldownButton;
                pulldownButton.ToolTip = "Инструменты для аннотирования светильников";

                // Add commands to the pull-down button
                AddPushButton(pulldownButton,
                    "LightFixtureAnnotation",
                    "Выбрать помещение",
                    "Inspirem.LightFixtureAnnotationCommand",
                    "Создает аннотацию светильников в выбранном помещении",
                    "Эта команда позволяет выбрать помещение и автоматически создать аннотацию, содержащую количество и типы светильников в этом помещении.",
                    "LightIcon.png");

                AddPushButton(pulldownButton,
                    "AllLightFixtureAnnotation",
                    "Все помещения",
                    "Inspirem.AllLightFixtureAnnotationCommand",
                    "Создает аннотации светильников для всех помещений",
                    "Автоматически создает аннотации для всех помещений.",
                    "AllLightsIcon.png");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", ex.Message);
                return Result.Failed;
            }
        }

        private static void AddPushButton(PulldownButton pulldownButton,
                                        string name,
                                        string text,
                                        string command,
                                        string toolTip,
                                        string longDescription,
                                        string iconName)
        {
            PushButtonData buttonData = new PushButtonData(
                name,
                text,
                thisAssemblyPath,
                command);

            buttonData.ToolTip = toolTip;
            buttonData.LongDescription = longDescription;

            // Set the button image
            try
            {
                string iconPath = Path.Combine(Path.GetDirectoryName(thisAssemblyPath), "Resources", iconName);
                if (File.Exists(iconPath))
                {
                    buttonData.Image = new BitmapImage(new Uri(iconPath));
                }
                else
                {
                    // Try to load from embedded resources
                    Uri uri = new Uri($"pack://application:,,,/{Assembly.GetExecutingAssembly().GetName().Name};component/Resources/{iconName}");
                    buttonData.Image = new BitmapImage(uri);
                }
            }
            catch
            {
                // Use default image if icon loading fails
            }

            pulldownButton.AddPushButton(buttonData);
        }

        private static void AddLightFixtureAnnotationButton(RibbonPanel panel)
        {
            var buttonData = new PushButtonData(
                "LightFixtureAnnotation",
                "Аннотация\nсветильников",
                thisAssemblyPath,
                "LightFixtureAnnotation.LightFixtureAnnotationCommand");

            buttonData.ToolTip = "Создает аннотацию светильников в выбранном помещении";
            buttonData.LongDescription = "Эта команда позволяет выбрать помещение и автоматически создать аннотацию, " +
                                       "содержащую количество и типы светильников в этом помещении.";

            SetButtonImage(buttonData, "LightIcon.png");

            panel.AddItem(buttonData);
        }

        private static void AddAllLightFixtureAnnotationButton(RibbonPanel panel)
        {
            var buttonData = new PushButtonData(
                "AllLightFixtureAnnotation",
                "Аннотация\nвсех светильников",
                thisAssemblyPath,
                "LightFixtureAnnotation.AllLightFixtureAnnotationCommand");

            buttonData.ToolTip = "Создает аннотации светильников для всех помещений";
            buttonData.LongDescription = "Автоматически создает аннотации для всех помещений в проекте.";

            SetButtonImage(buttonData, "AllLightsIcon.png");

            panel.AddItem(buttonData);
        }

        private static void SetButtonImage(PushButtonData buttonData, string iconName)
        {
            try
            {
                string iconPath = Path.Combine(Path.GetDirectoryName(thisAssemblyPath), "Resources", iconName);
                if (File.Exists(iconPath))
                {
                    buttonData.LargeImage = new BitmapImage(new Uri(iconPath));
                }
                else
                {
                    // Try to load from embedded resources
                    Uri uri = new Uri($"pack://application:,,,/{Assembly.GetExecutingAssembly().GetName().Name};component/Resources/{iconName}");
                    buttonData.LargeImage = new BitmapImage(uri);
                }
            }
            catch
            {
                // Use default image if icon loading fails
            }
        }
    }

    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            return RibbonCreator.OnStartup(application);
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}