using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using pluginspirem;

namespace LightFixtureAnnotation
{
    public class LightFixtureAnnotation
    {
        public static void CreateAnnotation(SpatialElement selectedSpace, Document doc, UIDocument uiDoc)
        {
            var lightFixtures = LightFixture.GetFixtures(selectedSpace, doc);
            var textNotes = CreateAnnotation(lightFixtures, selectedSpace, doc);
        }

        private static XYZ GetCenterPoint(SpatialElement space)
        {
            BoundingBoxXYZ bbox = space.get_BoundingBox(null);
            double centerX = (bbox.Min.X + bbox.Max.X) / 2;
            double centerY = (bbox.Min.Y + bbox.Max.Y) / 2;
            double centerZ = (bbox.Min.Z + bbox.Max.Z) / 2;
            return new XYZ(centerX, centerY, centerZ);
        }
        public static XYZ GetInsertionPoint(
            SpatialElement space,
            double offsetX = -.0,
            double offsetY = 0.0,
            double offsetZ = 0.0)
        {
            XYZ centerPoint = GetCenterPoint(space);
            return new XYZ(
                centerPoint.X + offsetX,
                centerPoint.Y + offsetY,
                centerPoint.Z + offsetZ);
        }

        private static List<TextNote> CreateAnnotation(ICollection<Element> lightFixtures, SpatialElement selectedSpace, Document doc)
        {
            const double verticalSpacing = -3.0;
            double currentYOffset = 4.0;
            var textNotes = new List<TextNote>();

            using (var trans = new Transaction(doc, "Create Fixture Annotation"))
            {
                trans.Start();

                var textNoteType = new FilteredElementCollector(doc)
                    .OfClass(typeof(TextNoteType))
                    .FirstElement() as TextNoteType;

                lightFixtures
                    .GroupBy(f => new {
                        FamilyName = string.IsNullOrEmpty(f.Name) ? "Светильник" : f.Name,
                        TypeName = (f as FamilyInstance)?.Symbol.Name ?? "Без типоразмера",
                        Height = LightFixture.GetFixtureHeight(f),
                        Power = LightFixture.GetFixturePower(f)
                    })
                    .OrderBy(g => g.Key.FamilyName)
                    .ThenBy(g => g.Key.TypeName)
                    .ThenBy(g => g.Key.Height)
                    .ThenBy(g => g.Key.Power)
                    .Select(g => new
                    {
                        Count = g.Count(),
                        g.Key.FamilyName,
                        g.Key.TypeName,
                        g.Key.Height,
                        g.Key.Power
                    })
                    .ToList()
                    .ForEach(fixture =>
                    {
                        var sign = fixture.Height >= 0 ? "+" : "-";
                        var insertionPoint = GetInsertionPoint(selectedSpace, offsetY: currentYOffset);
                        var annotationText = $"{fixture.Count} - {fixture.TypeName} - {fixture.Power}\n{sign}{Math.Abs(fixture.Height)} мм";
                        var textNote = CreateTextNote(doc, insertionPoint, annotationText);
                        textNotes.Add(textNote);

                        currentYOffset += verticalSpacing;
                    });

                trans.Commit();
            }

            return textNotes;
        }
        private static TextNote CreateTextNote(Document doc, XYZ position, string text)
        {
            TextNoteType textType = GetTextNoteType(doc);
            TextNote textNote = null;
            using (Transaction t = new Transaction(doc, "Create Text Note"))
            {
                textNote = TextNote.Create(doc, doc.ActiveView.Id, position, text, textType.Id);
            }

            ApplyAdditionalTextSettings(textNote, text);

            return textNote;
        }

        private static TextNoteType GetTextNoteType(Document doc, string typeName = "ISOCPEUR Annotation")
        {
            TextNoteType textType = new FilteredElementCollector(doc)
                .OfClass(typeof(TextNoteType))
                .Cast<TextNoteType>()
                .FirstOrDefault(x => x.Name == typeName);
            if (textType == null)
            {
                TextNoteType defaultType = new FilteredElementCollector(doc)
                    .OfClass(typeof(TextNoteType))
                    .Cast<TextNoteType>()
                    .First();
                    textType = defaultType.Duplicate(typeName) as TextNoteType;

                    textType.Name = typeName;

                    textType.get_Parameter(BuiltInParameter.TEXT_FONT).Set("ISOCPEUR");

                    textType.get_Parameter(BuiltInParameter.TEXT_SIZE).Set(2.5 / 304.8);

                    textType.get_Parameter(BuiltInParameter.TEXT_WIDTH_SCALE).Set(0.8);

                    //textType.get_Parameter(BuiltInParameter.TEXT_ALIGN_HORZ).Set((int)TextAlignFlags.TEF_ALIGN_CENTER);

            }

            return textType;    
        }

        private static void ApplyAdditionalTextSettings(TextNote textNote, string textContent)
        {
            FormattedText formattedText = textNote.GetFormattedText();

            TextRange underlineRange = GetUnderlineRange(textContent);

            formattedText.SetUnderlineStatus(underlineRange, true);

            textNote.HorizontalAlignment = HorizontalTextAlignment.Center;

            textNote.SetFormattedText(formattedText);
        }

        private static TextRange GetUnderlineRange(string text)
        {
            int newLinePos = text.IndexOf('\n');
            return newLinePos == -1 ? new TextRange(0, text.Length) : new TextRange(0, newLinePos);
        }
    }
}