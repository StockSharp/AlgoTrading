# Supertrend-Signal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie eröffnet Positionen, wenn der Schlusskurs die SuperTrend-Linie kreuzt. Ein Long-Trade wird platziert, wenn der Preis über die Linie steigt, und ein Short-Trade wird eröffnet, wenn der Preis darunter fällt. Entgegengesetzte Signale schließen und kehren bestehende Positionen um.

Der SuperTrend-Indikator verwendet die Average True Range (ATR), um dem Preis zu folgen und den vorherrschenden Trend zu definieren. Parameter ermöglichen die Konfiguration der ATR-Periode, des Multiplikators und des Kerzen-Zeitrahmens.

## Details

- **Einstiegskriterien**:
  - Long: Schlusskurs kreuzt SuperTrend von unten
  - Short: Schlusskurs kreuzt SuperTrend von oben
- **Long/Short**: Long und Short
- **Ausstiegskriterien**:
  - Entgegengesetzter SuperTrend-Kreuzung
- **Stops**: Keine
- **Standardwerte**:
  - `AtrPeriod` = 5
  - `Multiplier` = 3
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: SuperTrend (ATR-basiert)
  - Stops: Nein
  - Komplexität: Anfänger
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Keine
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
