.stat fájl:
minden sor egy generalas, ;-vel tagolt értékek

VERSION;SEED;VALID15;VALID20;FIDESZ_COUNT;OSSZEFOGAS_COUNT;JOBBIK_COUNT;LMP_COUNT;SIMILARITY
18 x { // minden választoi kerulethez adatok
	;GYOZTES;WRONG_AREA_CHANCE;FIDESZ_SZAVAZOK;OSSZEFOGAS_SZAVAZOK;JOBBIK_SZAVAZOK;LMP_SZAVAZOK;MEGJELENT;OSSZES
}

VERSION: azonositja a statisztika verziojat verziojat (double)
SEED: az adott generalast azonosito veletlen kezdoertek (int)
VALID15: a generalas teljesiti-e a 15%-os populacios kovetelmenyt (0 vagy 1)
VALID20: a generalas teljesiti-e a 20%-os populacios kovetelmenyt (0 vagy 1)
FIDESZ_COUNT: Fidesz által nyert keruletek szama (int)
OSSZEFOGAS_COUNT:  Osszefogas által nyert keruletek szama (int)
JOBBIK_COUNT: Jobbik által nyert keruletek szama (int)
LMP_COUNT: Lmp által nyert keruletek szama (int)
SIMILARITY: a körök hányadrésze tartozik ugyanahhoz a sorszámú kerülethez, mint eredetileg (double, 0-1)
GYOZTES: az adott kerulet nyertese ("FideszKDNP"/"Osszefogas"/"Jobbik"/"LMP")
WRONG_AREA_CHANCE: random walk alapjan a kerulethez tartozo korok hanyad resze tartozik rosz kerulethez (double, 0-1)
FIDESZ_SZAVAZOK: hanyan szavaztak a fideszre az adott keruletben (int)
OSSZEFOGAS_SZAVAZOK: hanyan szavaztak az osszefogasra az adott keruletben (int)
JOBBIK_SZAVAZOK: hanyan szavaztak a jobbikra az adott keruletben (int)
LMP_SZAVAZOK: hanyan szavaztak az lmpre az adott keruletben (int)
MEGJELENT: osszesen hanyan szavaztak az adott keruletben (int)
OSSZES: osszesen hany szavazo tartozott az adott kerulethez (int)