# Color BB Candles-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet Bollinger-Bänder, um Kerzen in bullische, bärische oder neutrale Zonen einzuteilen. Sie eröffnet eine Long-Position, wenn der Preis oberhalb des oberen Bandes schließt, eine Short-Position, wenn der Preis unterhalb des unteren Bandes schließt, und schließt jede Position, wenn der Preis zwischen die Bänder zurückkehrt.

## Details

- **Einstiegskriterien**:
  - **Long**: Der Schlusskurs kreuzt das obere Band von außen nach oben.
  - **Short**: Der Schlusskurs kreuzt das untere Band von außen nach unten.
- **Ausstiegskriterien**: Der Preis kehrt zwischen die obere und untere Band zurück.
- **Indikatoren**: Bollinger-Bänder.
- **Standardwerte**:
  - `BollingerPeriod` = 100
  - `BollingerDeviation` = 1.0
  - `CandleType` = 4-Stunden-Zeitrahmen
- **Richtung**: Long und Short.
- **Stops**: Keine.
- **Komplexität**: Moderat.
- **Zeitrahmen**: Mittelfristig.
