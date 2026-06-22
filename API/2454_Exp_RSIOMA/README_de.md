# Exp RSIOMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Exp RSIOMA-Strategie verwendet den RSI des gleitenden Durchschnitts (RSIOMA)-Indikator, um Trendumkehrungen und Ausbrüche zu handeln. RSI-Werte werden durch einen zusätzlichen gleitenden Durchschnitt geglättet, um eine Signallinie und ein Histogramm zu bilden. Die Strategie unterstützt vier Modi:

1. **Breakdown** – handelt, wenn RSI konfigurierte Hoch-/Niedrig-Niveaus kreuzt.
2. **HistTwist** – handelt, wenn das Histogramm die Richtung wechselt.
3. **SignalTwist** – handelt, wenn die Signallinie die Richtung wechselt.
4. **HistDisposition** – handelt, wenn das Histogramm die Signallinie kreuzt.

Positionen können für Long- und Short-Seiten unabhängig geöffnet oder geschlossen werden.

## Details

- **Einstiegskriterien**: abhängig vom `Mode`
- **Long/Short**: beide
- **Ausstiegskriterien**: Gegensignal
- **Stops**: keine
- **Standardwerte**:
  - `CandleType` = 4 hour
  - `RsiPeriod` = 14
  - `SignalPeriod` = 21
  - `HighLevel` = 20
  - `LowLevel` = -20
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: RSI
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
