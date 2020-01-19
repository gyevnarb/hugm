library(redist)

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

  # Load approximate square distances
  ssdist <- as.matrix(read.csv("ssdist.csv", sep=";", header=FALSE))

  ## Save dastaset
  bud <- list(adjobj=adjobj, popvec=popvec, initcds=initcds, results=results, ssdist=ssdist)
  save(bud, file=path)
}

find_params <- function(nsims=10000, ndists=18) {
  load("budapest.RData")
  params <- expand.grid(eprob = c(.01, .05, .1), lambda = c(0, 1, 2, 5, 10), beta = c(0, 5, 10, 50, 250, 750, 2500))

  p <- redist.findparams(adjobj=bud$adjobj,
    popvec=bud$popvec,
    nsims=nsims, ndists=ndists,
    initcds=bud$initcds,
    params=params
  )
}

run_simulation <- function(nsims=10000, ndists=18, popcons=0.15, seed=1, nloop=1, savename="bud") {
  load("budapest.RData")
  set.seed(seed)

  bud_alg <- redist.mcmc(adjobj=bud$adjobj,
    popvec=bud$popvec,
    nsims=nsims, ndists=ndists,
    initcds=bud$initcds,
    popcons=popcons,
    ssdmat=bud$ssdist,
    beta=9,
    constraint="compact",
    nloop=nloop,
    savename=savename
  )

  write.csv(t(bud_alg$partitions), "partitions.csv", row.names = FALSE)
  #save(bud_alg, file="bud_alg.RData")

  if (nloop > 1) {
    bud_out <- redist.combine(
      savename=savename,
      nsims=nsims,
      nloop=nloop,
      nthin=10,
      nunits=length(bud$adjobj)
    )
  }

  #save(bud_out, file="bud_out.RData")
}

run_analysis <- function(savename="bud", nsims=10000, nloop=10) {
  load("budapest.RData")

  bud_out <- redist.combine(
    savename=savename,
    nsims=nsims,
    nloop=nloop,
    nthin=10,
    nunits=length(bud$adjobj)
  )

  bud_dmi <- redist.segcalc(bud_out, bud$results$FideszKDNP, bud$popvec)

  plot.new()
  lines(density(bud_dmi, from = 0, to = 1), col = "red", lty = 5)

  #redist.diagplot(bud_dmi, plot = "trace")
  #redist.diagplot(bud_dmi, plot = "autocorr")
  #redist.diagplot(bud_dmi, plot = "densplot")
  #redist.diagplot(bud_dmi, plot = "mean")
}

# create_dataset()
run_simulation()
# run_analysis()
# find_params()
