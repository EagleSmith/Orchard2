using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fluid;
using Orchard.ContentManagement.Display.ContentDisplay;
using Orchard.ContentManagement.MetaData;
using Orchard.DisplayManagement.ModelBinding;
using Orchard.DisplayManagement.Views;
using Orchard.Liquid;
using Orchard.Markdown.Model;
using Orchard.Markdown.Settings;
using Orchard.Markdown.ViewModels;

namespace Orchard.Markdown.Drivers
{
    public class MarkdownPartDisplay : ContentPartDisplayDriver<MarkdownPart>
    {
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly ILiquidTemplateManager _liquidTemplatemanager;

        public MarkdownPartDisplay(
            IContentDefinitionManager contentDefinitionManager,
            ILiquidTemplateManager liquidTemplatemanager)
        {
            _contentDefinitionManager = contentDefinitionManager;
            _liquidTemplatemanager = liquidTemplatemanager;
        }

        public override IDisplayResult Display(MarkdownPart markdownPart)
        {
            return Combine(
                Shape<MarkdownPartViewModel>("MarkdownPart", m => BuildViewModel(m, markdownPart))
                    .Location("Detail", "Content:10"),
                Shape<MarkdownPartViewModel>("MarkdownPart_Summary", m => BuildViewModel(m, markdownPart))
                    .Location("Summary", "Content:10")
            );
        }

        public override IDisplayResult Edit(MarkdownPart markdownPart)
        {
            return Shape<MarkdownPartViewModel>("MarkdownPart_Edit", m => BuildViewModel(m, markdownPart));
        }

        public override async Task<IDisplayResult> UpdateAsync(MarkdownPart model, IUpdateModel updater)
        {
            await updater.TryUpdateModelAsync(model, Prefix, t => t.Markdown);

            return Edit(model);
        }

        private async Task BuildViewModel(MarkdownPartViewModel model, MarkdownPart markdownPart)
        {
            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(markdownPart.ContentItem.ContentType);
            var contentTypePartDefinition = contentTypeDefinition.Parts.FirstOrDefault(p => p.PartDefinition.Name == nameof(MarkdownPart));
            var settings = contentTypePartDefinition.GetSettings<MarkdownPartSettings>();

            var templateContext = new TemplateContext();
            templateContext.SetValue("ContentItem", markdownPart.ContentItem);
            templateContext.MemberAccessStrategy.Register<MarkdownPartViewModel>();

            using (var writer = new StringWriter())
            {
                await _liquidTemplatemanager.RenderAsync(markdownPart.Markdown, writer, NullEncoder.Default, templateContext);
                model.Markdown = writer.ToString();
                model.Html = Markdig.Markdown.ToHtml(model.Markdown ?? "");
            }

            model.MarkdownPart = markdownPart;
            model.TypePartSettings = settings;
        }
    }
}
