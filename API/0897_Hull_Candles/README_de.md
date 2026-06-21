# Hull Candles Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Hull Candles ist eine einfache Trendfolge-Strategie, die einen Hull Moving Average des Durchschnittspreises (OHLC4) verwendet. Wenn das HMA steigt und der Schlusskurs über seiner SMA liegt, werden Long-Positionen eröffnet; wenn das HMA fällt und der Schlusskurs unter seiner SMA liegt, werden Short-Positionen eröffnet.

## Details
- **Daten**: Preiskerzen.
- **Einstiegskriterien**:
  - **Long**: HMA steigt und Schlusskurs > SMA.
  - **Short**: HMA fällt und Schlusskurs < SMA.
- **Ausstiegskriterien**: Umgekehrtes Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - `BodyLength` = 10
  - `SmaLength` = 1
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long/Short
  - Indikatoren: HMA, SMA
  - Komplexität: Niedrig
  - Risikolevel: Hoch
