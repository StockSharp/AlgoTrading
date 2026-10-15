# Triple EMA Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf drei einfachen gleitenden Durchschnitten.
Ein Long-Trade wird eröffnet, wenn der kurze SMA den mittleren SMA von unten kreuzt, während alle drei aufwärts ausgerichtet sind.
Ein Short-Trade wird beim entgegengesetzten Crossover und Ausrichtung eröffnet.
Wenn der Preis den kurzen SMA zurückkreuzt, wird die Position geschlossen.

## Details

- **Einstiegskriterien**: Crossovers von SMA1 und SMA2 mit Trendfilter.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Preis kreuzt SMA1 oder Schutz-Stops.
- **Stops**: Ja.
- **Standardwerte**:
  - `Sma1Period` = 9
  - `Sma2Period` = 21
  - `Sma3Period` = 55
  - `StopLossTicks` = 200
  - `TakeProfitTicks` = 200
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: SMA
  - Stops: Fest
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Keine
  - Risikolevel: Mittel
