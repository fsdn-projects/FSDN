# FSDN

[![License][license-image]][license-url]

FSDN is a web application that uses [F# API Search](https://github.com/hafuu/FSharpApiSearch) library.
F# API Search library supports the standard signature of F# with some extentions.
This document describes the F# API Search library specific formats.

## Query format specifications

### Supported API signatures

| API signature | Query example |
|:--------------|:--------------|
| Functions and values in modules | `int -> string` |
| Fields of records and structs | `Ref<'a> => 'a` |
| Methods and properties | `'a list -> int` <br> or <br> `'a list => int` |
| Constructors | `string -> Uri` |
| Names (function and method names) | `head : 'a list -> 'a` |
| Active patterns | <code>(&#124;&#124;) : ... -> Expr -> ?</code> |

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

### Search members

#### Instance members

To search instance members, the query should be formatted as `receiver -> signature`.

To find methods that accept one argument, use `receiver -> arg -> returnType` format.

To find methods that accept multiple arguments, use `receiver -> arg1 -> arg2 -> returnType` or `receiver -> arg1 * arg2 -> returnType`.
By default, tuple style method arguments (`arg1 * arg2`) and curried style (`arg1 -> arg2`) are treated as identical.
If you want to distinguish between them, uncheck `ignore-argstyle` option.

To find properties, use `receiver -> propertyType`.
To find indexed properties, use `receiver -> index -> propertyType`.

`receiver -> signature` searches both instance members and functions.
However, when you use `=>` instead of `->`, it searches instance members only.

For instance members, the specified query matches the following special cases:

1. it matches `arg -> receiver -> returnType`.
2. a query to search parameterless members (`receiver => propertyType`) also matches instance methods which signature is `receiver => unit -> propertyType`.

The following query:

````
string => int
````

illustrates an example of these special rules.
This query matches the following methods and functions:

````
System.String.Length: int
Microsoft.FSharp.Core.String.length: string -> int
System.String.GetHashCode: unit -> int
````

The first result `System.String.Length` matches exactly.
In addition, `Microsoft.FSharp.Core.String.length` and `System.String.GetHashCode` are also retuned
because the 1st and 2nd special rules are applied respectively.

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

## Search Options

### `strict` option

When this option is enabled and when performing wildcard search,
each of the wildcards are distinguished by its name.
For instance, `?a -> ?a` matches `int -> int`,
but `?a -> ?b` doesn't match `int -> int`.
If this option is disabled, `?a -> ?b` matches `int -> int`.

### `similarity` option

When this option is enabled, type parameters match concrete type names, and vice-versa.
The results will be ordered by its similarity.
In addition, type constraint will be considered significant.

### `ignore-argstyle` option

When this option is enabled, the difference between curried style parameter signature (`arg1 -> arg2 -> returnType`)
and tuple style (`arg1 * arg2 -> returnType`) are ignored.
These styles are considered as identical.

## Current Build Status

- Windows [![Build status](https://ci.appveyor.com/api/projects/status/2joteb64gcot01ro/branch/master?svg=true)](https://ci.appveyor.com/project/pocketberserker/fsdn/branch/master)
- Linux/OSX [![Build Status](https://travis-ci.org/fsdn-projects/FSDN.svg?branch=master)](https://travis-ci.org/fsdn-projects/FSDN)

## Required Tools

- Node.js >= 6.x
- F# 4.0
- .NET Framework 4.6 or Mono >= 4.2.2

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
./build.sh mono='/usr/local/Cellar/mono/4.2.3/'
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
