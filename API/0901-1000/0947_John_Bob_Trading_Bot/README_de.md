# John Bob Trading Bot Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ausbruchsstrategie, die 50-Bar-Hoch-/Tiefniveaus mit einfacher Fair-Value-Gap-Erkennung kombiniert. Eröffnet fünf skalierte Orders mit ATR-basiertem Stop-Loss und mehreren Take-Profit-Niveaus.

## Details

- **Einstiegskriterien**:
  - Long: Preis kreuzt über das 50-Bar-Tief oder ein bullisches Fair-Value-Gap erscheint
  - Short: Preis kreuzt unter das 50-Bar-Hoch oder ein bärisches Fair-Value-Gap erscheint
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Preis erreicht eines der fünf Take-Profit-Niveaus
  - Preis trifft den ATR-basierten Stop-Loss
- **Stops**: ATR-Multiplikator
- **Standardwerte**:
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: ATR, Highest, Lowest
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
