using System.Collections.Generic;

namespace VisitorGenerator
{
    public partial class VisitorTemplate
    {
        public VisitorTemplateContext Context { get; }

        public VisitorTemplate( VisitorTemplateContext context )
        {
            Context = context;
        }

        public class VisitorTemplateContext
        {
            public string Namespace { get; }
            public string TypeName { get; }
            public string VisitDynamicTypeName { get; }
            public IReadOnlyCollection<string> VisitTypeNames { get; }

            public VisitorTemplateContext(
                string @namespace,
                string typeName,
                string visitDynamicTypeName,
                IEnumerable<string> visitTypeNames )
            {
                Namespace            = @namespace;
                TypeName             = typeName;
                VisitDynamicTypeName = visitDynamicTypeName;
                VisitTypeNames       = new List<string>( visitTypeNames );
            }
        }
    }
}