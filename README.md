# FSDN

[![License][license-image]][license-url]

FSDN is a web application that uses [F# API Search](https://github.com/hafuu/FSharpApiSearch) library.
F# API Search library supports the standard signature of F# with some extentions.
This document describes the F# API Search library specific formats.

## Search Options

### `respect-name-difference` option

When this option is enabled and when performing wildcard search,
each of the wildcards are distinguished by its name.
For instance, `?a -> ?a` matches `int -> int`,
but `?a -> ?b` doesn't match `int -> int`.
If this option is disabled, `?a -> ?b` matches `int -> int`.

### `greedy-matching` option

When this option is enabled, type parameters match concrete type names, and vice-versa.
The results will be ordered by its similarity.
In addition, type constraint will be considered significant.

### `ignore-parameter-style` option

When this option is enabled, the difference between curried style parameter signature (`arg1 -> arg2 -> returnType`)
and tuple style (`arg1 * arg2 -> returnType`) are ignored.
These styles are considered as identical.

### `ignore-case` option

When this option is enabled, API name and type name matching are case-insensitive.

### `swap-order` option

When this option is enabled, APIs that has swapped order of parameters or of tuple elements are searched as well.
For instance, `a -> b -> c` matches `b -> a -> c` because `a` and `b` are swapped.

### `complement` option

When this option is enabled, it complements missing arguments or tuple elements.
For instance, `a * c` matches `a * c` and also `a * b * c` where `b` includes `a` and `c`.

### `single-letter-as-variable` option

When this option is enabled, it treats one letter type name as type variable name.
For instance, `t list` is equal to `'t list`.

## Query format specifications of F#

### Supported API signatures

| API signature | Query example |
|:--------------|:--------------|
| Functions and values in modules | `int -> string` |
| Fields of records and structs | `Ref<'a> -> 'a` |
| Descriminated Union | `'a -> Option<'a>` |
| Members | `'a list -> int` |
| Constructors | `Uri : _`<br>`Uri.new : _`<br>`Uri..ctor : _` |
| Names (function and member names) | `head : 'a list -> 'a`<br>`head` |
| Active patterns | <code>(&#124;&#124;) : ... -> Expr -> ?</code> |
| Type, Type Abbreviation and Module | `List<'T>` |
| Computation Expressions | `{ let! } : Async<'T>` |

### Search by name

To search by name, the query should be formatted as `name : signature`.
If you don't want to specify the signature explicitly, use `_`, instead.

The following query:

````
id : 'a -> 'a
````

shows the following result:

````
Microsoft.FSharp.Core.Operators.id: 'T -> 'T
````

And the following query:

````
choose : _
````

shows the followings:

````
Microsoft.FSharp.Collections.Array.choose: ('T -> option<'U>) -> 'T[] -> 'U[]
Microsoft.FSharp.Collections.ArrayModule.Parallel.choose: ('T -> option<'U>) -> 'T[] -> 'U[]
Microsoft.FSharp.Collections.List.choose: ('T -> option<'U>) -> list<'T> -> list<'U>
Microsoft.FSharp.Collections.Seq.choose: ('T -> option<'U>) -> seq<'T> -> seq<'U>
Microsoft.FSharp.Control.Event.choose: ('T -> option<'U>) -> IEvent<'Del, 'T> -> IEvent<'U>
when 'Del : delegate and 'Del :> Delegate
Microsoft.FSharp.Control.Observable.choose: ('T -> option<'U>) -> IObservable<'T> -> IObservable<'U>
````

You can search by partial matche if you use asterisk.
For instance, to find all functions in `FSharp.Core.String` module, use `FSharp.Core.String.* : _`.

### Wildcard

By default, FSDN doesn't return results that match type parameters, such as `'a`, with concrete type names, such as `int`.
To find them, use wildcard: `?`.

````
? -> list<?> -> ?
````

This query shows the following results:

````
Microsoft.FSharp.Collections.List.append: list<'T> -> list<'T> -> list<'T>
Microsoft.FSharp.Collections.List.averageBy: ('T -> 'U) -> list<'T> -> 'U
when 'U : (static member op_Addition : 'U * 'U -> 'U) and 'U : (static member DivideByInt : 'U * int -> 'U) and 'U : (static member get_Zero : unit -> 'U)
Microsoft.FSharp.Collections.List.choose: ('T -> option<'U>) -> list<'T> -> list<'U>
Microsoft.FSharp.Collections.List.chunkBySize: int -> list<'T> -> list<list<'T>>
Microsoft.FSharp.Collections.List.collect: ('T -> list<'U>) -> list<'T> -> list<'U>
Microsoft.FSharp.Collections.List.contains: 'T -> list<'T> -> bool
when 'T : equality
Microsoft.FSharp.Collections.List.countBy: ('T -> 'Key) -> list<'T> -> list<'Key * int>
when 'Key : equality
Microsoft.FSharp.Collections.List.distinctBy: ('T -> 'Key) -> list<'T> -> list<'T>
when 'Key : equality
(snip)
````

If you want to specify the same type in several places, use "named wildcard".
For instance, when the following query

````
? -> ?
````

matches the following signatures:

````
'a -> 'a
int -> int
'a -> int
int -> string
````

and if you specify named wildcard as follows:

````
?a -> ?a
````

this doesn't match either `'a -> int` or `int -> string`.

#### Search subtypes
To search subtypes of the specified base type or interface, the query should be formatted as `#type`.

`type` indicates a base type name or an interface name.
You can not use type parameters and wildcards for `type`.

For instance, `? -> #seq<'t>` can search for functions that return subtype of `seq<'t>`, such as `List<'T>`, `IList<'T>` and `'T[]`.

### Search members

#### Instance members

To search instance members, the query should be formatted as `receiver -> signature`.

To find methods that accept one argument, use `receiver -> arg -> returnType` format.

To find methods that accept multiple arguments, use `receiver -> arg1 -> arg2 -> returnType` or `receiver -> arg1 * arg2 -> returnType`.
By default, tuple style method arguments (`arg1 * arg2`) and curried style (`arg1 -> arg2`) are treated as identical.
If you want to distinguish between them, uncheck `ignore-argstyle` option.

To find properties, use `receiver -> propertyType`.
To find indexed properties, use `receiver -> index -> propertyType`.

#### Static members

Static members can be found by using the same query for functions and values in modules.
As the same with instance methods, both `arg1 -> arg2 -> returnType` and `arg1 * arg2 -> returnType`
can be used to find static methods that accepts multiple arguments.

### Active patterns

To search active patterns, the query should be formatted as `(||) : (args ->) inputType -> returnType`.

To find partial active patterns, use `(|_|) : (args ->) inputType -> returnType` format.

`inputType` indicates a type handled by an active pattern.
For instance, to find active patterns for `Expr`, use `(||) : ... -> Expr -> ?`.

`args` indicates parameters of an active pattern.
To find active patterns that accepts multiple arguments, use `(||) : arg1 -> arg2 -> inputType -> returnType`.
To find active patterns that accepts no arguments, use `(||) : inputType -> returnType`.
To find active patterns that accepts zero or more arguments, use `...` keyword as `args`: `(||) : ... -> inputType -> returnType`.

`returnType` indicates a return type of an active pattern.
This must be different between active patterns each of which supports one case, multiple cases, and is partial active pattern,
and `option<_>` or `Choice<_,...,_>` must be specified.
Usually a wildcard (`?`) is recommended for `returnType`.

### Computation Expressions

To search computation expression builders, the query should be formatted as `{ syntax } : type`.
It searches all builders that support specified syntax and type.

`let!`, `yield`, `yield!`, `return`, `return!`, `use`, `use!`, `if/then`, `for`, `while`, `try/with`, `try/finally` and custom operations can be specified as the `syntax`.
To specify multiple syntaxes, use semicolon (`;`) separated value: `{ s1; s2 } : type`.

## Current Build Status

- Windows [![Build status](https://ci.appveyor.com/api/projects/status/2joteb64gcot01ro/branch/master?svg=true)](https://ci.appveyor.com/project/pocketberserker/fsdn/branch/master)
- Linux/OSX [![Build Status](https://travis-ci.org/fsdn-projects/FSDN.svg?branch=master)](https://travis-ci.org/fsdn-projects/FSDN)

## Required Tools

- Node.js >= 7.x
- F# 4.1
- .NET Framework 4.6 or Mono >= 4.8.0

## How To build

### Windows

```
./build.cmd
```

### Linux

```
./build.sh
```

or

```
./build.sh mono='/path/to/mono/home'
```

### OS X with homebrew

```
./build.sh mono='/usr/local/Cellar/mono/4.8.0/'
```

### Docker

```
./build-in-docker.sh
```

## How to run

### Windows

```
./bin/FSDN/FSDN.exe --home-directory ./bin/FSDN
```

### Mono

```
mono ./bin/FSDN/FSDN.exe --home-directory ./bin/FSDN
```

### Docker

```
docker run -d -p 8083:8083 --name=fsdn fsdn mono /app/FSDN/FSDN.exe --home-directory /app/FSDN/
```

[license-url]: https://github.com/fsdn-projects/FSDN/blob/master/LICENSE
[license-image]: https://img.shields.io/github/license/fsdn-projects/FSDN.svg
