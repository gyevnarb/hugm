create_dataset <- function(path = "budapest.RData") {
  ## Load adjacency list
  x <- scan("maplist.csv", what="", sep="\n")
  y <- strsplit(x, ",")
  adjobj <- type.convert(y)

  ## Load populations
  popvec <- as.numeric(unlist(read.csv("mappop.csv", header=FALSE)))

  ## Initial district labels
  initcds <- as.numeric(unlist(read.csv("initcds.csv", header=FALSE)))

  ## Load election results
  results <- read.table("votes.csv", sep=",", header=TRUE)

  ## Save dastaset
  bud <- list(adjobj=adjobj, popvec=popvec, inicds=initcds, results=results)
  save(bud, file=path)
  bud
}

run_analysis <- function(nsims=10000, ndists=18, popcons=0.15, seed=1) {
  d <- load("budapest.RData")
  set.seed(seed)

  params <- expand.grid(eprob=c(.01, .05, .1))
  p <- redist.findparams(adjobj=bud$adjobj,
    popvec=bud$popvec,
    nsims=nsims, ndists=ndists,
    initcds=bud$initcds,
    params=params
  )

  bud_alg <- redist.mcmc(adjobj=bud$adjobj,
    popvec=bud$popvec,
    nsims=bud$nsims, ndists=bud$ndists,
    initcds=bud$initcds,
    popcons=bud$popcons)

  bud_dmi <- redist.segcalc(bud_alg, bud$results$FideszKDNP, bud$popvec)
}

create_dataset()
