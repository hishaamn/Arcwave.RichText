using Arcwave.RichText.Models;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.Data.Items;
using Sitecore.DependencyInjection;
using Sitecore.Security.Accounts;
using Sitecore.XA.Foundation.Abstractions.Configuration;
using Sitecore.XA.Foundation.Editing;
using Sitecore.XA.Foundation.TokenResolution.Pipelines.ResolveTokens;
using Sitecore;
using Sitecore.Configuration;

namespace Arcwave.RichText.Pipelines.ResolveTokens
{
    public class ResolveEditingTokens : ResolveTokensProcessor
    {
        public string FieldDescription => "Description";

        public RteMappingConfiguration ProfileMapping { get; set; }
        
        public RteMapping RteMapping { get; set; }

        public override void Process(ResolveTokensArgs args)
        {
            var query = args.Query;
            
            var token = "$arcwaveDefaultRichTextProfile";
            
            var contextItem = args.ContextItem;
            
            var escapeSpaces = args.EscapeSpaces;
            
            this.RteMapping = this.GetRteMapping(query);
            
            if (this.RteMapping != null && !string.IsNullOrEmpty(this.RteMapping.Token))
            {
                token = this.RteMapping.Token;
            }

            query = args.Query = this.ReplaceTokenWithValue(query, token, () => this.GetRichTextProfile(contextItem, false));
        }

        protected virtual string GetRichTextProfile(Item contextItem, bool escapeSpaces)
        {
            var richTextPath = this.GetRichTextEditorPath(Context.User.Roles);

            return richTextPath ?? ServiceLocator.ServiceProvider.GetService<IConfiguration<EditingConfiguration>>().GetConfiguration().DefaultRichTextProfile;
        }

        private string GetRichTextEditorPath(UserRoles roles)
        {
            if (this.RteMapping == null)
            {
                return null;
            }

            var applyOnAdmin = Settings.GetSetting("Arcwave.RichText.ApplyTokenOnAdmin", "false");

            if (Context.User.IsAdministrator && !applyOnAdmin.Equals("true"))
            {
                return null;
            }

            var listRole = roles.Select(roleObject => roleObject.Name.ToLower()).ToList();
            
            var match = this.RteMapping.GetContain(listRole);
            
            if (!string.IsNullOrEmpty(match))
            {
                return $"{this.RteMapping.RtePath} {match}";
            }
            else
            {
                var defaultRt = this.ProfileMapping.MappingList.Find(element => element.Name.ToLower().Equals("defaultrichtext"));

                return defaultRt?.RtePath;
            }
        }

        private RteMapping GetRteMapping(string query) => this.ProfileMapping.MappingList.Find(mapping => query.Contains(mapping.Token));
    }
}
