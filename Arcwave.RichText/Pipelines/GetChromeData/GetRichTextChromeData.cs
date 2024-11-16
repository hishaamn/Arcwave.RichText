using Microsoft.Extensions.DependencyInjection;
using Sitecore.Abstractions;
using Sitecore.Data.Fields;
using Sitecore.DependencyInjection;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.GetChromeData;
using Sitecore.Pipelines;
using Sitecore.XA.Foundation.Abstractions;
using Sitecore.XA.Foundation.Horizon.Context;
using Sitecore.XA.Foundation.Multisite.Extensions;
using Sitecore.XA.Foundation.SitecoreExtensions.Pipelines.GetRichTextProfile;
using Sitecore.XA.Foundation.SitecoreExtensions.Repositories;
using Sitecore.XA.Foundation.TokenResolution;
using System;
using System.Linq;
using Sitecore.XA.Foundation.SitecoreExtensions.Extensions;
using Sitecore.Data.Items;

namespace Arcwave.RichText.Pipelines.GetChromeData
{
    public class GetRichTextChromeData : GetChromeDataProcessor
    {
        protected readonly IContext Context;
        
        protected readonly ITokenResolver TokenResolver;
        
        protected readonly IDatabaseRepository DatabaseRepository;

        protected readonly IHorizonContext HorizonContext = ServiceProviderServiceExtensions.GetService<IHorizonContext>(ServiceLocator.ServiceProvider);

        public GetRichTextChromeData(BaseClient client, IContext context, ITokenResolver tokenResolver, IDatabaseRepository databaseRepository) : base(client)
        {
            this.Context = context;
            this.DatabaseRepository = databaseRepository;
            this.TokenResolver = tokenResolver;
        }

        public override void Process(GetChromeDataArgs args)
        {
            if (!SiteExtensions.IsSxaSite(ServiceProviderServiceExtensions.GetService<IContext>(ServiceLocator.ServiceProvider).Site) || !"field".Equals(args.ChromeType, StringComparison.OrdinalIgnoreCase) || this.HorizonContext.IsHorizonEditing || !args.CustomData.ContainsKey("field") || !(args.CustomData["field"] is Field field) || !field.TypeKey.Is("rich text"))
            {
                return;
            }

            var richTextProfilePath = this.GetRichTextProfilePath(field);

            Log.Error($"[RTE]:: richTextProfilePath: {richTextProfilePath}", this);

            if (string.IsNullOrEmpty(richTextProfilePath))
            {
                return;
            }

            var obj = this.DatabaseRepository.GetDatabase("core").GetItem(richTextProfilePath);
            
            if (obj == null)
            {
                Log.Warn(string.Format("Couldn't find RichText profile: '{0}' for field: {1}", (object)richTextProfilePath, (object)field.ID), (object)this);
            }
            else
            {
                var rootItem = obj.Children.SingleOrDefault(c => c.Name.Is("WebEdit Buttons"));

                if (rootItem == null)
                {
                    return;
                }

                foreach (var button1 in this.GetButtons(rootItem))
                {
                    var button = button1;

                    if (!args.ChromeData.Commands.Any(command => this.IsCommandEqual(command, button)))
                    {
                        this.AddButtonToChromeData(button, args);
                    }
                }
            }
        }

        protected virtual string GetRichTextProfilePath(Field field)
        {
            var args = new GetRichTextProfileArgs()
            {
                ProfilePath = field.Source,
                ItemID = this.Context.Item.ID.ToString()
            };

            CorePipeline.Run("getRichTextProfile", args);

            Log.Error($"[RTE]:: args.ProfilePath: {args.ProfilePath}", this);

            return args.ProfilePath;
        }

        protected virtual bool IsCommandEqual(WebEditButton command, WebEditButton button) => command.Click.Is(button.Click) && command.Header.Is(button.Header) && command.Tooltip.Is(button.Tooltip);
    }
}
