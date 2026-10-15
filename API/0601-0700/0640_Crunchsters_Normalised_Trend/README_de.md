# Crunchsters Normalisierte Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Renditen normalisiert und eine Hull Moving Average auf den kumulierten normalisierten Kurs anwendet.
Geht long, wenn der normalisierte Kurs über die HMA kreuzt, und short, wenn er darunter kreuzt.

Tests zeigen eine durchschnittliche Jahresrendite von ca. 105%. Sie funktioniert am besten auf dem Kryptomarkt.

Normalisierte Renditen ermöglichen die Skalierung des Kurses nach der jüngsten Volatilität. Ein ATR-basierter Stop verwaltet das Risiko.

## Details

- **Einstiegskriterien**:
  - Long: `nPrice` kreuzt über `HMA`
  - Short: `nPrice` kreuzt unter `HMA`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Gegenseitiger Crossover oder ATR-Stop
- **Stops**: ATR-basiert mit `StopMultiple`
- **Standardwerte**:
  - `NormPeriod` = 14
  - `HmaPeriod` = 100
  - `HmaOffset` = 0
  - `StopMultiple` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Hull Moving Average, Standard Deviation, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
