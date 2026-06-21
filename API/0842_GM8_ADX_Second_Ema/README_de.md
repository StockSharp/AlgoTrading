# GM-8 und ADX-Strategie mit zweitem EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie eröffnet Trades, wenn der Kurs einen GM-8-SMA kreuzt und sich mit einem zweiten EMA ausrichtet, während der ADX einen starken Trend bestätigt.

## Details

- **Einstiegskriterien**:
  - **Long**: Kurs kreuzt den SMA von unten und schließt über dem SMA und dem zweiten EMA, während ADX über dem Schwellenwert liegt.
  - **Short**: Kurs kreuzt den SMA von oben und schließt unter dem SMA und dem zweiten EMA, während ADX über dem Schwellenwert liegt.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - **Long**: Kurs kreuzt den SMA von oben nach unten.
  - **Short**: Kurs kreuzt den SMA von unten nach oben.
- **Stops**: Verwendet StartProtection.
- **Standardwerte**:
  - `GM Period` = 15
  - `Second EMA Period` = 59
  - `ADX Period` = 8
  - `ADX Threshold` = 34
  - `Candle Type` = 15m
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: SMA, EMA, ADX
  - Stops: Ja
  - Komplexität: Niedrig
  - Zeitrahmen: Kurzfristig

