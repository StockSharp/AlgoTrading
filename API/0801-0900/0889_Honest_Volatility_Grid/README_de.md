# Strategie Honest Volatility Grid
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf mehreren Keltner-Kanal-Ebenen zum Aufbau eines Volatilitätsgitters. Sie skaliert in Long- und Short-Positionen über vordefinierte Bänder ein und steigt über entgegengesetzte Ebenen oder einen rohen Stop aus.

## Details

- **Einstiegskriterien**: Kurs erreicht konfigurierte Keltner-Kanal-Ebenen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetzter Kanal oder roher Stop.
- **Stops**: Optionaler roher Stop.
- **Standardwerte**:
  - `EmaPeriod` = 200
  - `Multiplier` = 1.0
  - `LEntry1Level` = -2
  - `SEntry1Level` = 2
  - `RawStopLevel` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Gitter
  - Richtung: Beide
  - Indikatoren: EMA, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
