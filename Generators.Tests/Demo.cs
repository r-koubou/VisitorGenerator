namespace VisitorGenerator.Testing
{
    public interface INode {}
    public interface IChildNodeA : INode {}
    public interface IChildNodeB : INode {}
    public interface IChildNodeC : INode {}

    [VisitTo( typeof( IChildNodeA ) )]
    [VisitTo( typeof( IChildNodeB ) )]
    [VisitTo( typeof( IChildNodeC ) )]
    [VisitDynamicFrom( typeof( INode ) )]
    public partial interface IMyVisitor<T> {}
}