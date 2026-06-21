# Verbesserte Bollinger-Bands-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kauft, wenn der Preis unter das untere Bollinger Band fällt, während der Markt über einem 200-Perioden-EMA bleibt.  
Ein Stop Loss wird bei `Einstieg - ATR * Stop` gesetzt, und nachdem der Preis `ATR * Trail` über den Einstieg gestiegen ist, wird das mittlere Band zum Trailing-Ziel.

## Details

- **Einstiegskriterien**: `Low > EMA` und `Low <= Unteres Band`.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Schlusskurs unter dem mittleren Band nach Aktivierung des Trailings oder Tiefstkurs unter dem Stop.
- **Stops**: ATR-basierter Stop Loss.
- **Standardwerte**:
  - Bollinger-Periode = 20
  - EMA-Periode = 200
  - ATR-Periode = 14
  - Stop ATR = 1.75
  - Trail ATR = 2.25

