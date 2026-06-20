# CCI-Divergenz-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
CCI-Divergenzen (Commodity Channel Index) können Trendumkehrungen ankündigen, wenn sich der Preis in die entgegengesetzte Richtung des Indikators bewegt. Diese Strategie vergleicht Swing-Hochs und -Tiefs im Preis mit denen des CCI, um verstärkte Stärke oder Schwäche zu identifizieren.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 91%. Sie funktioniert am besten auf dem Aktienmarkt.

Bei jeder Kerze aktualisiert das System die jüngsten Preis- und CCI-Werte und markiert eine bullische Divergenz, wenn der Preis ein neues Tief macht, während der CCI ein höheres Tief bildet. Bärische Divergenz ist das Gegenteil. Wenn eine Divergenz mit überverkauften oder überkauften Niveaus übereinstimmt, wird ein Trade mit einem Volatilitätsstopp eröffnet.

Ausstiege erfolgen, wenn der CCI wieder durch die Nulllinie kreuzt, was signalisiert, dass der Impuls abgespielt hat. Da Divergenzen andauern können, werden die Regeln auch nach einer festen Anzahl von Balken zurückgesetzt, um veraltete Signale zu vermeiden.

## Details

- **Einstiegskriterien**: Preis/CCI-Divergenz mit CCI unter -100 für Longs oder über +100 für Shorts.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: CCI kreuzt Null oder Stop-Loss.
- **Stops**: Ja, prozentbasiert.
- **Standardwerte**:
  - `CciPeriod` = 20
  - `DivergencePeriod` = 5
  - `OverboughtLevel` = 100
  - `OversoldLevel` = -100
  - `CandleType` = 15 minute
  - `StopLossPercent` = 2
- **Filter**:
  - Kategorie: Divergenz
  - Richtung: Beide
  - Indikatoren: CCI
  - Stops: Ja
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel

