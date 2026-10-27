// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Controls;
using Barbatos.Wpf.AquariusValee.Xaml;

namespace Barbatos.Wpf.AquariusValee.UnitTests;

public class FieldTemplateSelectorTests
{
    private static FrameworkElement ContainerWithTemplates(params (string key, DataTemplate template)[] templates)
    {
        var element = new Border();
        foreach (var (key, template) in templates)
            element.Resources[key] = template;
        return element;
    }

    [Fact]
    public void SelectTemplatePrefersUIHintOverDataTypeHint()
    {
        StaThread.Run(() =>
        {
            var multiline = new DataTemplate();
            var creditCard = new DataTemplate();
            var container = ContainerWithTemplates(("MultilineText", multiline), ("CreditCard", creditCard));

            var field = new Field<string>("Notes", "") { UIHint = "MultilineText", DataTypeHint = DataType.CreditCard };

            var selector = new FieldTemplateSelector();
            Assert.Same(multiline, selector.SelectTemplate(field, container));
        });
    }

    [Fact]
    public void SelectTemplateFallsBackToDataTypeHintsEnumNameWhenNoUIHintIsSet()
    {
        StaThread.Run(() =>
        {
            var creditCard = new DataTemplate();
            var container = ContainerWithTemplates(("CreditCard", creditCard), ("Default", new DataTemplate()));

            var field = new Field<string>("CardNumber", "") { DataTypeHint = DataType.CreditCard };

            var selector = new FieldTemplateSelector();
            Assert.Same(creditCard, selector.SelectTemplate(field, container));
        });
    }

    [Fact]
    public void SelectTemplateFallsBackToDefaultWhenNeitherHintHasAMatchingResource()
    {
        StaThread.Run(() =>
        {
            var defaultTemplate = new DataTemplate();
            var container = ContainerWithTemplates(("Default", defaultTemplate));

            var field = new Field<string>("PlainField", "");

            var selector = new FieldTemplateSelector();
            Assert.Same(defaultTemplate, selector.SelectTemplate(field, container));
        });
    }

    [Fact]
    public void SelectTemplateFallsBackToDefaultWhenAHintIsSetButNoMatchingResourceExists()
    {
        StaThread.Run(() =>
        {
            var defaultTemplate = new DataTemplate();
            // No "MultilineText" resource registered, even though the field asks for one.
            var container = ContainerWithTemplates(("Default", defaultTemplate));

            var field = new Field<string>("Notes", "") { UIHint = "MultilineText" };

            var selector = new FieldTemplateSelector();
            Assert.Same(defaultTemplate, selector.SelectTemplate(field, container));
        });
    }

    [Fact]
    public void SelectTemplateDefersToBaseImplementationForNonFieldItems()
    {
        StaThread.Run(() =>
        {
            var container = ContainerWithTemplates(("Default", new DataTemplate()));

            var selector = new FieldTemplateSelector();

            Assert.Null(selector.SelectTemplate("not a field", container));
        });
    }
}
