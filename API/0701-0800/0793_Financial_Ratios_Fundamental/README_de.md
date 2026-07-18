# Fundamentale Finanzkennzahlen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie analysiert vierteljährliche Finanzkennzahlen, um die Fundamentaldaten eines Unternehmens zu bewerten. Sie betrachtet die Current Ratio, die Zinsdeckung, den Verbindlichkeitsumschlag und die Bruttomarge und eröffnet Long-Positionen, wenn sich eine dieser Kennzahlen im Vergleich zum Vorquartal verbessert.

## Details

- **Einstiegskriterien**:
  - **Long**: `currentRatio > previousCurrent` ODER `interestCoverage < previousInterest` ODER `payableTurnover > previousPayable` ODER `grossMargin > previousGross`.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - **Long**: `currentRatio < previousCurrent` ODER `interestCoverage > previousInterest` ODER `payableTurnover < previousPayable` ODER `grossMargin < previousGross`.
- **Stops**: Nein.
- **Standardwerte**:
  - `Candle Type` = Tageskerzen.
- **Filter**:
  - Kategorie: Fundamental
  - Richtung: Nur Long
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Langfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
