# Visitor generator

A C# Visitor pattern interface generator with C# 9.0 Source Generator



## Installation

from [Nuget](https://www.nuget.org/packages/VisitorGenerator/)



## Usage

Use a  `VisitTo` attribute to generate "Visit" method.

```c#
public interface INode {}
public interface IChildNodeA : INode {}
public interface IChildNodeB : INode {}
public interface IChildNodeC : INode {}

[VisitTo(typeof(IChildNodeA))]
[VisitTo(typeof(IChildNodeB))]
[VisitTo(typeof(IChildNodeC))]
public partial interface IChildNodeVisitor<T>{} // note:Template must be <T>
```

will generate following

```c#
public partial interface IChildNodeVisitor<T>
{
    public T Visit( IChildNodeA obj );
    public T Visit( IChildNodeB obj );
    public T Visit( IChildNodeC obj );
}
```



If use a `VisitDynamicFrom`, generate a type dynamic synatax code.

```c#
public interface INode {}
public interface IChildNodeA : INode {}
public interface IChildNodeB : INode {}
public interface IChildNodeC : INode {}

[VisitTo(typeof(IChildNodeA))]
[VisitTo(typeof(IChildNodeB))]
[VisitTo(typeof(IChildNodeC))]
[VisitDynamicFrom(typeof(INode))]
public partial interface IChildNodeVisitor<T>{} // note:Template must be <T>
```

will generate following

```c#
public partial interface IChildNodeVisitor<T>
{
    public T Visit( IChildNodeA obj );
    public T Visit( IChildNodeB obj );
    public T Visit( IChildNodeC obj );

    public T Visit( INode obj )
    {
        return Visit( (dynamic)obj );
    }
}
```