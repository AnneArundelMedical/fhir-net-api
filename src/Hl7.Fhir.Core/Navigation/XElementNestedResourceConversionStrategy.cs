using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Hl7.Fhir.Navigation
{
    internal class XElementNestedResourceConversionStrategy : XElementConversionStrategy
    {
        public override bool HandlesDocNode(XObject docNode)
        {
            // This strategy handles a subclass of "normal" XElement nodes
            return base.HandlesDocNode(docNode) && hasNestedResource((XElement)docNode);
        }

        public override FhirNavigationTree ConstructTreeNode(XObject docNode, FhirNavigationTree parent)
        {
            var result = base.ConstructTreeNode(docNode, parent);
            var nestedResourceName = ((XElement)docNode).Elements().First().Name.LocalName;

            result.AddAnnotation(new XmlRenderHints { NestedResourceName = nestedResourceName });

            return result;
        }
        public override IEnumerable<XObject> SelectChildren(XObject docNode, FhirNavigationTree treeNode)
        {
            var children = base.SelectChildren(docNode, treeNode);

            // For a nested resource, the intermediate node (which is a child with the name of a resource) should disappear,
            // the nested nodes taking its place
            foreach(var child in children)
            {
                if (child is XElement && isNestedResource((XElement)child))
                {
                    foreach (var containedNode in ((XElement)child).Nodes())
                        yield return containedNode;
                }
                else
                    yield return child;
            }
         }

        //Try to determine whether this node has a nested resource. This is most probable if
        // a) The childe has no siblings
        // b) The node name starts with a capitalized letter
        // c) The node name is one of the known resources
        private static bool hasNestedResource(XElement element)
        {
            if (element.HasElements)
            {
                element = element.Elements().First();

                return isNestedResource(element);
            }

            return false;
        }

        private static bool isNestedResource(XElement element)
        {
            return Char.IsUpper(element.Name.LocalName[0]) && ModelInfo.IsKnownResource(element.Name.LocalName);
        }
    }
}