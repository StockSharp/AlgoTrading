# Benutzerdefinierter Signal-Oszillator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die die Differenz zwischen zwei Preissignalen nutzt. Sie geht long, wenn der Oszillator die Nulllinie von unten kreuzt, und short, wenn er sie von oben kreuzt. Im Nur-Long-Modus schließen negative Kreuzungen die Position.

## Details

- **Einstiegskriterien**: Oszillator kreuzt die Nulllinie.
- **Long/Short**: Beide Richtungen oder nur Long.
- **Ausstiegskriterien**: Entgegengesetztes Signal oder Nulllinienkreuzung im Nur-Long-Modus.
- **Stops**: Nein.
- **Standardwerte**:
  - `LongOnly` = false
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
