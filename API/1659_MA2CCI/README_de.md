# Strategie MA2CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Gleitender-Durchschnitt-Kreuzungsstrategie, bestätigt durch CCI. Verwendet ATR für den Stop-Loss.

## Details

- **Einstiegskriterien**:
  - Long, wenn der schnelle SMA den langsamen SMA nach oben kreuzt und CCI über 0 kreuzt.
  - Short, wenn der schnelle SMA den langsamen SMA nach unten kreuzt und CCI unter 0 kreuzt.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Umgekehrte Kreuzung oder Stop-Loss bei 1 ATR vom Einstieg.
- **Stops**: ATR-basierter Stop beim Einstiegspreis ± ATR.
- **Standardwerte**:
  - `FastMaPeriod` = 4
  - `SlowMaPeriod` = 8
  - `CciPeriod` = 4
  - `AtrPeriod` = 4
  - `CandleType` = 1 Minute
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: SMA, CCI, ATR
  - Stops: ATR
  - Komplexität: Anfänger
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
