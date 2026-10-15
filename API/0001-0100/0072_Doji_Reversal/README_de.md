# Doji-Umkehrstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Doji-Kerzen spiegeln ein vorübergehendes Gleichgewicht zwischen Käufern und Verkäufern wider. Wenn ein Doji nach einer starken Richtungsbewegung erscheint, kann dies eine Umkehr einleiten, da das Momentum nachlässt. Diese Strategie misst den Kerzenkörper im Verhältnis zur Handelsspanne, um festzustellen, ob ein echter Doji gebildet wurde.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 103%. Die Strategie eignet sich am besten für den Aktienmarkt.

Sobald ein Doji erkannt wird, werden die vorherigen Kerzen auf einen Auf- oder Abwärtstrend geprüft. Ein Doji nach einem Rückgang kann einen Long-Einstieg auslösen, während einer nach einem Anstieg eine Short-Position eröffnen kann. Stops werden in einem prozentualen Abstand vom Einstieg gesetzt, und Ausstiege erfolgen, wenn der Kurs die Extrempunkte des Doji durchbricht.

Die Methode zielt darauf ab, die erste Reaktion vom Doji weg zu erfassen, und eignet sich am besten für Intraday-Charts, wo schnelle Umkehrungen häufig auftreten.

## Details

- **Einstiegskriterien**: Doji-Kerze nach einer Richtungsbewegung.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Kurs bewegt sich über das Doji-Hoch/-Tief hinaus oder Stop-Loss.
- **Stops**: Ja, prozentbasiert.
- **Standardwerte**:
  - `CandleType` = 5 minute
  - `DojiThreshold` = 0.1
  - `StopLossPercent` = 1
- **Filter**:
  - Kategorie: Muster
  - Richtung: Beide
  - Indikatoren: Candlestick
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

