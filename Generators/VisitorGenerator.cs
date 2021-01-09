using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace VisitorGenerator
{
    public class QualifiedTypeName
    {
        public string Qualified { get; }
        public string Namespace { get; }
        public string ClassName { get; }

        public QualifiedTypeName( string qualifiedName )
        {
            var s = qualifiedName.Split( '.' );
            var @namespace = "";
            var className = s[ s.Length - 1 ];

            if( s.Length > 1 )
            {
                @namespace = s[ 0 ];
                for( var i = 1; i < s.Length - 1; i++ )
                {
                    @namespace += $".{s[ i ]}";
                }
            }

            Qualified = qualifiedName;
            Namespace = @namespace;
            ClassName = className;
        }
    }

    [Generator]
    public class VisitorGenerator : ISourceGenerator
    {
        public void Initialize( GeneratorInitializationContext context )
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        private static readonly SourceText VisitToAttributeSource = SourceText.From(
            "using System;\r\n\r\nnamespace VisitorGenerator\r\n{\r\n    [System.AttributeUsage( AttributeTargets.Interface, AllowMultiple = true )]\r\n    public class VisitToAttribute : System.Attribute\r\n    {\r\n        public Type Type { get; }\r\n\r\n        public VisitToAttribute( Type type )\r\n        {\r\n            Type = type;\r\n        }\r\n    }\r\n}",
            Encoding.UTF8
        );

        private static readonly SourceText VisitDynamicFromAttributeSource = SourceText.From(
            "using System;\r\n\r\nnamespace VisitorGenerator\r\n{\r\n    [System.AttributeUsage( AttributeTargets.Interface, AllowMultiple = false )]\r\n    public class VisitDynamicFromAttribute : System.Attribute\r\n    {\r\n        public Type Type { get; }\r\n\r\n        public VisitDynamicFromAttribute( Type type )\r\n        {\r\n            Type = type;\r\n        }\r\n    }\r\n}",
            Encoding.UTF8
        );

        public void Execute( GeneratorExecutionContext context )
        {

            context.AddSource( "VisitToAttribute",  VisitToAttributeSource );
            context.AddSource( "VisitDynamicFromAttribute", VisitDynamicFromAttributeSource );

            if( !( context.SyntaxReceiver is SyntaxReceiver receiver ) )
            {
                return;
            }

            try
            {
                var parseOptions = ( context.Compilation as CSharpCompilation )?.SyntaxTrees[ 0 ].Options as CSharpParseOptions;
                var compilation = context.Compilation.AddSyntaxTrees( CSharpSyntaxTree.ParseText( VisitToAttributeSource, parseOptions ) )
                                         .AddSyntaxTrees( CSharpSyntaxTree.ParseText( VisitDynamicFromAttributeSource,      parseOptions ) );

                // Correct from symbol
                var visitToAttributeSymbol = compilation.GetTypeByMetadataName( "VisitorGenerator.VisitToAttribute" );
                var visitDynamicFromAttributeSymbol = compilation.GetTypeByMetadataName( "VisitorGenerator.VisitDynamicFromAttribute" );

                CorrectMultipleAttributeSymbols( receiver, compilation, visitToAttributeSymbol!, out var visitorElementClasses );

                var visitDynamicTypeName = CorrectAttributeSymbol( receiver, compilation, visitDynamicFromAttributeSymbol! );
                var mappingTable = CreateVisitorMapping( visitorElementClasses, visitToAttributeSymbol! );

                // Generate visitor code
                foreach( var qualified in mappingTable.Keys )
                {
                    // get namespace and typename from qualified name
                    var qualifiedTypeName = new QualifiedTypeName( qualified );

                    // Generate by T4
                    var ctx = new VisitorTemplate.VisitorTemplateContext(
                        qualifiedTypeName.Namespace,
                        qualifiedTypeName.ClassName,
                        visitDynamicTypeName,
                        mappingTable[ qualifiedTypeName.Qualified ]
                    );
                    var text = new VisitorTemplate( ctx ).TransformText();
                    context.AddSource( qualified, SourceText.From( text, Encoding.UTF8 ) );
                    //debugLogwriter.WriteLine( text );
                }
            }
            catch( Exception e )
            {
                Trace.WriteLine( e );
                //writer.WriteLine( e );
            }
        }

        private static void CorrectMultipleAttributeSymbols( SyntaxReceiver receiver, Compilation compilation, INamedTypeSymbol attributeSymbol, out List<INamedTypeSymbol> visitorElementClasses )
        {
            visitorElementClasses = new List<INamedTypeSymbol>();

            foreach( var clazz in receiver.Symbols )
            {
                var semantic = compilation.GetSemanticModel( clazz.SyntaxTree );
                var declaredSymbol = semantic.GetDeclaredSymbol( clazz );

                if( declaredSymbol!.GetAttributes().Any( x => SymbolEqualityComparer.Default.Equals( x.AttributeClass, attributeSymbol ) ) )
                {
                    visitorElementClasses.Add( declaredSymbol );
                }
            }
        }

        private static string CorrectAttributeSymbol( SyntaxReceiver receiver, Compilation compilation, INamedTypeSymbol attributeSymbol )
        {
            foreach( var clazz in receiver.Symbols )
            {
                var semantic = compilation.GetSemanticModel( clazz.SyntaxTree );
                var declaredSymbol = semantic.GetDeclaredSymbol( clazz );

                var acceptBaseType = declaredSymbol!.GetAttributes().FirstOrDefault( x => SymbolEqualityComparer.Default.Equals( x.AttributeClass, attributeSymbol ) );

                if( acceptBaseType != null )
                {
                    return acceptBaseType.ConstructorArguments[ 0 ].Value!.ToString();
                }
            }

            return string.Empty;
        }


        private static Dictionary<string, List<string>> CreateVisitorMapping( List<INamedTypeSymbol> visitorElementClasses, INamedTypeSymbol attributeSymbol )
        {
            var mappingTable = new Dictionary<string, List<string>>();

            foreach( var classSymbol in visitorElementClasses )
            {
                foreach( var attr in classSymbol.GetAttributes().Where( x => SymbolEqualityComparer.Default.Equals( x.AttributeClass, attributeSymbol ) ) )
                {
                    var visitorType = classSymbol.ToString();
                    var visitorTargetType = attr.ConstructorArguments[ 0 ].Value!.ToString();

                    visitorType = Regex.Replace( visitorType, @"<[^>]+>", "" );

                    if( !mappingTable.ContainsKey( visitorType! ) )
                    {
                        mappingTable[ visitorType! ] = new List<string>();
                    }

                    mappingTable[ visitorType! ].Add( visitorTargetType );
                }
            }

            return mappingTable;
        }

        class SyntaxReceiver : ISyntaxReceiver
        {
            public List<InterfaceDeclarationSyntax> Symbols { get; } = new List<InterfaceDeclarationSyntax>();
            public void OnVisitSyntaxNode( SyntaxNode syntaxNode )
            {
                if( syntaxNode is InterfaceDeclarationSyntax interfaceDeclarationSyntax &&
                    interfaceDeclarationSyntax.AttributeLists.Count > 0 )
                {
                    Symbols.Add( interfaceDeclarationSyntax );
                }
            }
        }
    }
}