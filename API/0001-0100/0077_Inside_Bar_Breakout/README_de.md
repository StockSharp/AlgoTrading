# Inside Bar Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine Inside Bar entsteht, wenn die Handelsspanne einer Kerze vollständig innerhalb des Hochs und Tiefs des vorherigen Balkens liegt. Sie signalisiert kurzfristige Unentschlossenheit, die zu einem Ausbruch führen kann, sobald der Kurs das Muster verlässt. Diese Strategie wartet auf diesen Ausbruch und handelt dann in Richtung der Expansion.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 118%. Die Strategie eignet sich am besten für den Aktienmarkt.

Jede neue Kerze wird mit der vorherigen verglichen. Erscheint eine Inside Bar, markiert das System ihr Hoch und Tief und beobachtet einen Schlusskurs außerhalb dieser Niveaus. Ein bullischer Ausbruch eröffnet eine Long-Position mit einem Stop unterhalb des Musters-Tiefs, während ein bearischer Ausbruch einen Short mit einem Stop oberhalb des Musters-Hochs auslöst.

Sollte der Kurs nicht sofort ausbrechen, verwaltet die Strategie bestehende Positionen, indem sie aussteigt, wenn die nächste Kerze gegen den Trade über die Extrempunkte des vorherigen Balkens hinausgeht.

## Details

- **Einstiegskriterien**: Ausbruch des Hochs oder Tiefs einer Inside Bar.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Kurs kreuzt das Extrem der vorherigen Kerze oder Stop-Loss.
- **Stops**: Ja, außerhalb des Musters platziert.
- **Standardwerte**:
  - `CandleType` = 5 minute
  - `StopLossPercent` = 1
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Candlestick
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

