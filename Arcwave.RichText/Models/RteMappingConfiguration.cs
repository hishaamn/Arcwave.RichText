using Sitecore.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Arcwave.RichText.Models
{
    public class RteMappingConfiguration
    {
        public List<RteMapping> MappingList { get; private set; }

        public RteMappingConfiguration() => this.MappingList = new List<RteMapping>();
        
        public void AddRteMappings(XmlNode profileMappingNode)
        {
            var rteMapping = new RteMapping(profileMappingNode);
        
            this.MappingList.Add(rteMapping);
        }
    }

    public class RteMapping
    {
        public string Name { get; set; }

        public string RtePath { get; set; }
        
        public Dictionary<string, List<string>> Roles { get; set; }
        
        public string Token { get; set; }
        
        public RteMapping(XmlNode profileMappingNode)
        {
            this.Roles = new Dictionary<string, List<string>>();

            this.PopulateProperty(profileMappingNode);
        }
        
        private void PopulateProperty(XmlNode rteMappping)
        {
            this.Name = XmlUtil.GetAttribute("name", rteMappping).ToLower();

            this.RtePath = XmlUtil.GetAttribute("rtePath", rteMappping);
            
            this.Token = XmlUtil.GetAttribute("token", rteMappping);
            
            this.Roles = this.ExtractRoles(XmlUtil.GetChildElements("roles", rteMappping));
        }
        
        private Dictionary<string, List<string>> ExtractRoles(IEnumerable<XmlNode> rolesNode)
        {
            var listRole = new Dictionary<string, List<string>>();

            foreach (var node in rolesNode)
            {
                var nodeRoles = XmlUtil.GetChildElements("role", node);
            
                var name = XmlUtil.GetAttribute("name", node);
                
                var roles = nodeRoles.Select(role => XmlUtil.GetValue(role).ToLower()).ToList();
                
                listRole.Add(name, roles);
            }
        
            return listRole;
        }
        
        public bool IsMatched(List<string> role)
        {
            foreach (var key in this.Roles.Keys)
            {
                var b = !(role.Except(this.Roles[key]).Any());
            
                if (b)
                {
                    return true;
                }
            }

            return false;
        }

        public string GetMatched(List<string> role)
        {
            foreach (var key in this.Roles.Keys)
            {
                var b = !(role.Except(this.Roles[key]).Any());

                if (b)
                {
                    return key;
                }
            }
            return string.Empty;
        }

        public string GetContain(List<string> role)
        {
            foreach (var key in this.Roles.Keys)
            {
                foreach (var value in this.Roles[key])
                {
                    var getMatching = role.Where(r => r.Contains(value)).FirstOrDefault();

                    if (!string.IsNullOrEmpty(getMatching))
                    {
                        return key;
                    }
                }
            }

            return string.Empty;
        }
    }
}
