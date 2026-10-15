# Bereinigtes Screener-Bibliothek
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Einfache Screener-Strategie, die den RSI über mehrere Symbole hinweg bewertet und Kauf- oder Verkaufsbewertungen ausgibt. Sie dient als Grundlage für den Aufbau benutzerdefinierter Multi-Asset-Screener.

## Details

- **Einstiegskriterien**: RSI-Werte werden für jedes Symbol gegen Schwellenwerte geprüft.
- **Long/Short**: Keine (nur Signale)
- **Ausstiegskriterien**: Keine
- **Stops**: Keine
- **Standardwerte**:
  - `RsiLength` = 14
  - `StrongThreshold` = 70m
  - `WeakThreshold` = 60m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filter**:
  - Kategorie: Screener
  - Richtung: N/A
  - Indikatoren: RSI
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: N/A
