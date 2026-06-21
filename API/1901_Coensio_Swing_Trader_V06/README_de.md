# Coensio Swing Trader V06-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert die Ausbruchslogik des ursprünglichen Coensio Swing Traders. Der Donchian-Kanal definiert dynamische Unterstützungs- und Widerstandsniveaus. Ein Trade wird eröffnet, wenn der Preis um einen konfigurierbaren Schwellenwert über das obere Band oder unter das untere Band ausbricht.

## Details

- **Einstieg**:
  - **Long**: Der Schlusskurs bricht über das obere Donchian-Band + `Entry Threshold` Pips.
  - **Short**: Der Schlusskurs bricht unter das untere Donchian-Band - `Entry Threshold` Pips.
- **Ausstiege**:
  - Fester `Stop Loss` und `Take Profit` in Pips gemessen vom Eintrittspreis.
  - Optionaler Wechsel zu Break-Even nach `Break Even` Pips Gewinn.
  - Optionaler Trailing Stop, der dem Preis nach Break-Even um `Trailing Step` Pips folgt.
- **Stops**: Stop-Loss, Take-Profit, Break-Even, Trailing Stop.
- **Standardwerte**:
  - `Channel Period` = 20
  - `Entry Threshold` = 15 Pips
  - `Stop Loss` = 50 Pips
  - `Take Profit` = 80 Pips
  - `Break Even` = 25 Pips
  - `Trailing Step` = 5 Pips
  - `Enable Trailing` = false
  - `Candle Type` = 15-Minuten-Kerzen
