## Load adjacency list
x <- scan("maplist.csv", what="", sep="\n")
y <- strsplit(x, ",")
adjobj <- type.convert(y)

## Load populations
popvec <- as.numeric(read.csv("mappop.csv", header=FALSE))

## Initial district labels
initcds = unlist(as.numeric(read.csv("initcds.csv", header=FALSE)))

## Number of simulations before save point
nsims = 10000

## Number of voting districts
ndists = 18

## Hard population constraint
popcons = 0.15

set.seed(1)

alg = redist.mcmc(adjobj=adjobj, popvec=popvec, nsims=nsims, ndists=ndists, initcds=initcds, popcons=popcons)