# Rock Trader Neuro-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die mit Bollinger Bändern und einem einfachen Neuron handelt.
Die letzten sieben Bollinger-Band-Breiten werden auf den Bereich [-1,1] normalisiert und
mit festen Gewichten kombiniert. Die gewichtete Summe wird durch eine hyperbolische
Tangens-Aktivierung geleitet. Eine negative Ausgabe öffnet eine Long-Position, während eine positive
Ausgabe eine Short-Position öffnet. Positionen werden durch Stop Loss oder Take Profit geschlossen.

## Details

- **Einstiegskriterien**:
  - Long: Neuronenausgabe < 0
  - Short: Neuronenausgabe > 0
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Stop Loss oder Take Profit erreicht
- **Stops**: Absoluter Preisabstand
- **Standardwerte**:
  - `StopLoss` = 30
  - `TakeProfit` = 100
  - `Lot` = 1
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Neural
  - Richtung: Beide
  - Indikatoren: Bollinger Bands
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Ja
  - Divergenz: Nein
  - Risikolevel: Mittel
