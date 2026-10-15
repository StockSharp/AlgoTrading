# OverHedge V2 Grid-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

OverHedge V2 ist ein abgesichertes Rastersystem, das Long- und Short-Positionen abwechselt und gleichzeitig die Handelsgröße nach jeder Füllung erhöht. Die Strategie analysiert die Beziehung zwischen einem schnellen und einem langsamen exponentiellen gleitenden Durchschnitt (EMA), um die vorherrschende Richtung für den nächsten Zyklus zu bestimmen. Sobald ein Zyklus beginnt, platziert der Algorithmus Marktaufträge, sobald der Preis vordefinierte Tunnelniveaus um den Startkurs herum erreicht. Das Gitter dehnt sich symmetrisch aus, sodass jedes neue Bein den schwebenden Verlust des vorherigen ausgleicht. Der Zyklus endet, wenn der gesamte offene Gewinn ein konfigurierbares Ziel überschreitet oder wenn der Händler manuell eine Abschaltung anfordert.

Die Implementierung führt getrennte Bilanzen für Long- und Short-Engagements und nutzt Live-Preise der Stufe 1, um neue Absicherungen auszulösen. Das Handelsvolumen wächst geometrisch entsprechend dem gewählten Multiplikator, der das Martingal-Risiko des ursprünglichen MetaTrader-Expertenberaters reproduziert. Da Aufträge zum Marktwert ausgeführt werden, passt sich das System automatisch an die Liquiditätsbedingungen an und behält dabei den in Punkten ausgedrückten Rasterabstand bei.

## Wie es funktioniert

1. **Richtungsfilter** – Die Strategie berechnet zwei EMAs für abgeschlossene Kerzen. Wenn der schnelle EMA über dem langsamen EMA liegt, beginnt der nächste Zyklus mit einer langen Tendenz; andernfalls beginnt es mit einer kurzen Voreingenommenheit.
2. **Zyklusinitialisierung** – Zu Beginn eines Zyklus zeichnet der Algorithmus den aktuellen Geldpreis auf und leitet zwei Tunnelgrenzen ab, die durch die konfigurierte Breite und den Live-Spread getrennt sind. Die erste Ordnung folgt der EMA-Vorspannung, und der entgegengesetzte Zweig wird im Tunnelabstand abgestuft.
3. **Rastererweiterung** – Bleibt der Preis gegenüber dem letzten Eintrag bestehen, werden abwechselnd weitere Marktaufträge ausgelöst (Kauf, Verkauf, Kauf, …). Jedes neue Bein multipliziert das vorherige Volumen mit dem Hedge-Multiplikator, sodass sich die Gesamtposition bei einer Umkehr schneller erholen kann.
4. **Gewinnernte** – Der Zyklus überwacht ständig nicht realisierte Gewinne anhand der besten Geld-/Briefkurse. Wenn der Zielwert erreicht ist oder der Bediener das Abschaltflag umschaltet, werden alle offenen Zweige aufgelöst und der Zyklus zurückgesetzt.
5. **Exposure Tracking** – Die Strategie behält den durchschnittlichen Preis und das durchschnittliche Volumen sowohl für Long- als auch für Short-Hedges bei, um den offenen Gewinn präzise zu berechnen und das Senden doppelter Aufträge zu vermeiden, während bestehende noch ausstehen.

## Standardparameter

- `Base Volume` = 0,1 Lots – anfängliche Handelsgröße für den ersten Rasterabschnitt.
- `Hedge Multiplier` = 2,0 – Volumenmultiplikator, der auf jede nachfolgende Etappe angewendet wird.
- `Tunnel Width (points)` = 20 – Zusätzlicher Abstand zwischen wechselnden Aufträgen über den aktuellen Spread hinaus.
- `Profit Target` = 100 – Nicht realisierter Gewinn in der Kontowährung, der das gesamte Raster schließt.
- `Short EMA` = 8 – Zeitraum des schnellen EMA, der zur Richtungserkennung verwendet wird.
- `Long EMA` = 21 – Zeitraum des langsamen EMA, der zur Richtungserkennung verwendet wird.
- `Candle Type` = 1 Minute – Zeitrahmen, der die EMA-Filter speist.
- `Shutdown Grid` = false – Bei „true“ beendet die Strategie sofort alle Abschnitte und stoppt den Handel.

## Notizen

- Das Raster funktioniert mit jedem Instrument, das Kurse der Stufe 1 (bester Geld-/Briefkurs) bereitstellt. Größere Spreads erhöhen automatisch die Tunnelgröße.
- Das Handelsvolumen wird mithilfe des Sicherheitsvolumenschritts normalisiert, um abgelehnte Aufträge zu vermeiden.
- Da das System ein Martingal-Größenschema verwendet, sind große Rückgänge möglich, wenn die Preistrends anhalten, ohne das Gewinnziel zu erreichen.
- Um den Handel nach einer Schließung wieder aufzunehmen, schalten Sie den Parameter wieder auf `false` um oder starten Sie die Strategie neu.
