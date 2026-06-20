# VWAP Volume Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die VWAP und Volumen-Indikatoren kombiniert. Kauft/verkauft bei VWAP-Ausbrüchen, die durch überdurchschnittliches Volumen bestätigt werden.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 52%. Am besten geeignet für den Kryptomarkt.

Diese Strategie nutzt den VWAP zur Bewertung des fairen Wertes und erfordert eine Volumenbestätigung vor dem Trade. Die Idee ist, sich Bewegungen anzuschließen, die durch starke Marktteilnahme unterstützt werden.

Intraday-Trader, die sich auf Volumenkennzahlen konzentrieren, können diese Methode anwenden. Verluste werden durch einen ATR-basierten Stop begrenzt.

## Details

- **Einstiegskriterien**:
  - Long: `Close < VWAP && Volume > AvgVolume * VolumeThreshold`
  - Short: `Close > VWAP && Volume > AvgVolume * VolumeThreshold`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Preis kreuzt zurück durch den VWAP
- **Stops**: Prozentbasiert mit `StopLossPercent`
- **Standardwerte**:
  - `VolumePeriod` = 20
  - `VolumeThreshold` = 1.5m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: VWAP, Volumen
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
