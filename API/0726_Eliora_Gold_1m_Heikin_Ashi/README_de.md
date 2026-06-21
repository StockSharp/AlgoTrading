# Eliora Gold 1m Heikin Ashi-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie verwendet Heikin Ashi-Kerzen auf einem Ein-Minuten-Zeitrahmen. Sie steigt bei starken trendkonformen Kerzen ein, wenn der Markt nicht konsolidiert, und erzwingt eine Abkühlphase zwischen Trades. Ausstiege werden durch einen ATR-basierten Trailing-Stop gesteuert.

## Details

- **Einstiegskriterien**: starke Heikin Ashi-Kerze in Trendrichtung, keine Konsolidierung, Volatilitätsfilter.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: ATR-Trailing-Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `AtrPeriod` = 14
  - `CooldownBars` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Heikin Ashi, ATR, SMA, Highest/Lowest
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
