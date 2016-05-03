# FSDN

[![License][license-image]][license-url]

## Current Build Status

- Windows [![Build status](https://ci.appveyor.com/api/projects/status/2joteb64gcot01ro/branch/master?svg=true)](https://ci.appveyor.com/project/pocketberserker/fsdn/branch/master)
- Linux/OSX [![Build Status](https://travis-ci.org/fsdn-projects/FSDN.svg?branch=master)](https://travis-ci.org/fsdn-projects/FSDN)

## Required Tools

- Node.js >= 5.x
- F# 4.0
- .NET Framework 4.6 or Mono >= 4.2.2

## How To build

### Windows

```
./build.cmd
```

## Linux

```
export MONO_HOME='/usr'
./build.sh
```

### OS X with homebrew

```
export MONO_HOME='/usr/local/Cellar/mono/4.2.3/'
./build.sh
```

[license-url]: https://github.com/fsdn-projects/FSDN/blob/master/LICENSE
[license-image]: https://img.shields.io/github/license/fsdn-projects/FSDN.svg

