# LMHyper: Largest Models for HyperLTL

This repository contains LMHyper - a satisfiability checker for $`\forall^1\exists^*`$ HyperLTL formulas.
See the corresponding paper for details:

> Deciding Hyperproperties Combined with Functional Specifications. Raven Beutner, David Carral, Bernd Finkbeiner, Jana Hofmann, Markus Krötzsch. LICS 2022 [1]


Clone this repository and **initialize all submodules** by running

```shell
git clone https://github.com/ravenbeutner/LMHyper
cd LMHyper
git submodule init
git submodule update
```

## Structure 

This repository is structured as follows:

- `src/` contains the source code of LMHyper (written in F#). 
- `app/` is the target folder for the build. The final LMHyper executable will be placed here.

## Build

This section contains instructions on how to build LMHyper from sources. 

### Dependencies

To build and run LMHyper, you need the following dependencies:

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download) (tested with version 8.0.204)
- [spot](https://spot.lrde.epita.fr/) (tested with version 2.11.6)

Install the .NET 8 SDK (see [here](https://dotnet.microsoft.com/en-us/download) for details).
Download and build _spot_ (details can be found [here](https://spot.lrde.epita.fr/)). 
You can place the _spot_ executables in any location of your choosing, and provide LMHyper with the _absolute_ path (see details below).

### Build LMHyper

To build LMHyper run the following (when in the main directory of this repository).

```shell
cd src/LMHyper
dotnet build -c "release" -o ../../app
cd ../..
```

Afterward, the `LMHyper` executable is located in the `app/` folder.

### Connect Spot

LMHyper requires the _ltl2tgba_ and _autfilt_ executable from the spot library.
LMHyper is designed such that it only needs the **absolute** path to these executables, so they can be installed and placed at whatever location fits best.
The absolute paths are specified in a `paths.json` configuration file. 
This file must be located in the *same* directory as the LMHyper executables (this convention makes it easy to find the config file, independent of the relative path LMHyper is called from). 
We provide a template file `app/paths.json` that **needs to be modified**. 
After having built _spot_, paste the absolute path to _spot_'s _ltl2tgba_ and _autfilt_ executables to the `paths.json` file. 
For example, if `/usr/bin/ltl2tgba` and `/usr/bin/autfilt` are the _ltl2tgba_, and _autfilt_ executables, respectively, the content of `app/paths.json` should be

```json
{
    "ltl2tgba": "/usr/bin/ltl2tgba", 
    "autfilt": "/usr/bin/autfilt"
}
```


# Run LMHyper

After you have built LMHyper and modified the configuration file you can use LMHyper by running the following
 
```shell
./app/LMHyper <options> <instance>
```

where `<instance>` is the HyperLTL formula, and `<options>` defines the command-line options. 
For example, `./app/LMHyper "forall pi. G ('a'_pi)"`.

Alteratively, you can also run 

```shell
./app/LMHyper <options> -f <instancePath>
```

where `<instancePath>` is the path to a file that contains the input instance (the HyperLTL formula), and `<options>` defines the command-line options. 


## Command Line Options 

Using the `--iter <n>` option, you can bound the number of iterations performed by LMHyper.
When using the `--log` option, LMHyper prints additional debug information. 

## HyperLTL

LMHyper supports HyperLTL formulas in an extension of [spot's](https://spot.lrde.epita.fr/) LTL format.
Concretely, a HyperLTL formula consists of an LTL body, preceded by a quantifier prefix of trace variables.
A trace variable (`<tvar>`) is any (non-reserved) sequence consisting of letters and digits (starting with a letter).

Formally, a HyperLTL formula has the form `<prefix> <body>`.

Here `<body>` can be one of the following:
- `1`: specifies the boolean constant true
- `0`: specifies the boolean constant false
- `"<ap>"_<tvar>`, where `<ap>` is an atomic proposition (AP), which can be any string not containing `"`. Note that APs always need to be escaped in `"`s.
- `'<ap>'_<tvar>`, where `<ap>` is an atomic proposition (AP), which can be any string not containing `'`. Note that APs always need to be escaped in `'`s.
- `(<body>)`
- `<body> <bopp> <body>`, where `<bopp>` can be `&` (conjunction), `|` (disjunction), `->` (implication), `<->` (equivalence), `U` (until operator), `W` (weak until operator), and `R` (release operator)
- `<uopp> <body>`, where `<uopp>` can be `!` (negation), `X` (next operator), `G` (globally), and `F` (eventually operator)

The operators follow the usual operator precedences. 
To avoid ambiguity, we recommend always placing parenthesis around each construct. 

The quantifier prefix `<prefix>` can be one of the following:

- The empty string
- Universal or existential trace quantification `forall <tvar>. <prefix>` and `exists <tvar>. <prefix>`. 

An example property is the following: 

```
forall pi. exists pii. G ("a"_pi <-> !"a"_pii)
```

or, using single quotes, 

```
forall pi. exists pii. G ('a'_pi <-> !'a'_pii)
```


# References

[1] Deciding Hyperproperties Combined with Functional Specifications. Raven Beutner, David Carral, Bernd Finkbeiner, Jana Hofmann, Markus Krötzsch. LICS 2022