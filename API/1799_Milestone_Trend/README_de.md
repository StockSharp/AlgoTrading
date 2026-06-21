# Milestone Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine StockSharp-Portierung des Expertenberaters Milestone 22.5. Sie handelt Rücksetzer innerhalb eines Trends, indem sie zwei geglättete gleitende Durchschnitte mit einem Volatilitäts- und Spike-Filter kombiniert. Wenn eine Kerze das Extrem der vorherigen Kerze durchbricht und der schnelle Durchschnitt die Bewegung bestätigt, wird eine Position in Richtung des dominanten Trends eröffnet. ATR verhindert den Handel in ruhigen Märkten und große Kerzenkörper werden als Spikes behandelt.

Backtests der ursprünglichen MQL-Version zeigen gute Leistung bei wichtigen Forex-Paaren. Die C#-Übersetzung legt Wert auf Klarheit und verwendet nur Marktorders für Ein- und Ausstiege.

## Details

- **Einstiegskriterien**:
  - Trendstärke zwischen `MinTrend` und `MaxTrend`.
  - Kerze durchbricht vorheriges Hoch oder Tief und schnelle SMA bestätigt.
  - ATR über `MinRange` und Kerzenkörper unter `CandleSpike`.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal schließt die Position.
- **Stops**: Nicht implementiert; entgegengesetztes Signal fungiert als Stop.
- **Standardwerte**:
  - `SlowMaPeriod` = 120
  - `FastMaPeriod` = 30
  - `AtrPeriod` = 14
  - `MinTrend` = 10
  - `MaxTrend` = 100
  - `MinRange` = 5
  - `CandleSpike` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: SMA, ATR
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
