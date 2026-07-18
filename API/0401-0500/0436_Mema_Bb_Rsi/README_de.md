# Multi-Timeframe EMA + BB + RSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kombiniert zwei exponentielle gleitende Durchschnitte, Bollinger Bands und RSI, um Bounces zu handeln. Long-Trades entstehen, wenn der Kurs nach dem Berühren des unteren Bandes über der schnellen EMA schließt. Short-Trades werden ausgelöst, wenn der Kurs nach dem Durchstechen des oberen Bandes unter der schnellen EMA schließt und RSI über 50 liegt.

Eine optionale Gewinnmitnahme schließt die Position nach einer benutzerdefinierten Anzahl von Kerzen, wenn sich der Kurs günstig entwickelt. Das System ist flexibel genug für Swing- oder Intraday-Trading und unterstützt das unabhängige Aktivieren oder Deaktivieren der Long- und Short-Seiten.

## Details

- **Einstiegskriterien**:
  - **Long**: Schluss über der schnellen EMA mit einem Tief, das das untere Bollinger Band durchsticht.
  - **Short**: Schluss unter der schnellen EMA mit einem Hoch, das das obere Band durchsticht, und RSI > 50.
- **Ausstiegskriterien**:
  - Long: RSI steigt über den Überverkauft-Level.
  - Short: Preis schließt unter dem unteren Band.
- **Indikatoren**:
  - Zwei EMAs (Perioden 10 und 55)
  - Bollinger Bands (Länge 20, Multiplikator 2)
  - RSI (Länge 14, Überverkauft 71)
- **Stops**: Optionales Gewinnziel nach X Kerzen; kein fester Stop-Loss.
- **Standardwerte**:
  - `Ma1Period` = 10
  - `Ma2Period` = 55
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
  - `RSILength` = 14
  - `RSIOversold` = 71
  - `XBars` = 12
- **Filter**:
  - Mean Reversion mit Trendfilter
  - Zeitrahmen: Konfigurierbar
  - Indikatoren: EMA, Bollinger Bands, RSI
  - Stops: Optional
  - Komplexität: Moderat
