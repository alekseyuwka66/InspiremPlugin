using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Annotations;
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
                RibbonPanel panel = application.CreateRibbonPanel(tabName, "Светильники");

                // Create a pull-down button to group related commands
                PulldownButtonData pulldownData = new PulldownButtonData(
                    "LightFixtureTools",
                    "Аннотации");

                PulldownButton pulldownButton = panel.AddItem(pulldownData) as PulldownButton;
                pulldownButton.ToolTip = "Инструменты для аннотирования светильников";

                SetButtonImage(pulldownButton, "Annotation.png");

                // Add commands to the pull-down button
                AddPushButton(pulldownButton,
                    "LightFixtureAnnotation",
                    "Выбрать помещение",
                    "Inspirem.LightFixtureAnnotationCommand",
                    "Создает аннотацию светильников в выбранном помещении",
                    "Эта команда позволяет выбрать помещение и автоматически создать аннотацию, содержащую количество и типы светильников в этом помещении.",
                    "Annotation.png");

                AddPushButton(pulldownButton,
                    "AllLightFixtureAnnotation",
                    "Все помещения",
                    "Inspirem.AllLightFixtureAnnotationCommand",
                    "Создает аннотации светильников для всех помещений",
                    "Автоматически создает аннотации для всех помещений.",
                    "Annotation.png");

                AddStandalonePushButton(panel,
                    "PlaceLightFixtures",
                    "Расставить светильники",
                    "Inspirem.PlaceLightFixturesCommand",
                    "Автоматически расставляет светильники в помещении",
                    "Эта команда автоматически размещает светильники в выбранном помещении согласно заданным параметрам.",
                    "Creation.png");

                AddStandalonePushButton(panel,
                    "PlaceDimensions",
                    "Расставить размеры",
                    "Inspirem.PlaceDimensionsCommand",
                    "Добавляет размеры для светильников",
                    "Эта команда автоматически добавляет размеры между светильниками и стенами.",
                    "Dimensions.png");

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

            var button = pulldownButton.AddPushButton(buttonData);
        }

        private static void AddStandalonePushButton(RibbonPanel panel,
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

            var button = panel.AddItem(buttonData) as PushButton;
        }

        private static void SetButtonImage(PulldownButton button, string iconName)
        {
            try
            {
                Uri uri = new Uri($"pack://application:,,,/pluginspirem;component/Icons/{iconName}");
                button.LargeImage = new BitmapImage(uri);
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