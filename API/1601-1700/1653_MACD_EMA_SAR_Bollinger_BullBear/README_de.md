# MACD EMA SAR Bollinger BullBear-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kombiniert MACD, EMA-Kreuzung, Parabolic SAR, Bollinger Bänder und Bulls/Bears Power Indikatoren. Handelt nur während aktiver Stunden.

## Details

- **Einstiegskriterien**:
  - **Long**: MACD < Signal, die letzten zwei Hochs unterhalb des oberen Bollinger Bandes, EMA3 > EMA34, SAR unterhalb des Preises, Bulls Power > 0 und fallend.
  - **Short**: MACD > Signal, EMA3 < EMA34, SAR oberhalb des Preises, Bears Power < 0 und steigend.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**:
  - Keine dedizierten Ausstiegsregeln; Position schließt beim entgegengesetzten Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - `MACD Fast` = 12
  - `MACD Slow` = 26
  - `MACD Signal` = 9
  - `Fast EMA Period` = 3
  - `Slow EMA Period` = 34
  - `Power Period` = 13
  - `SAR Step` = 0.02
  - `SAR Max` = 0.2
  - `Bollinger Period` = 20
  - `Bollinger Deviation` = 2.0
  - `Candle Type` = 15 Minuten
  - `Session Start` = 09:00
  - `Session End` = 17:00
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Mehrere
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
