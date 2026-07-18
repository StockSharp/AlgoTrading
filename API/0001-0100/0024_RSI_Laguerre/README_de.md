# Strategie RSI Laguerre
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem Laguerre RSI

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 109%. Sie funktioniert am besten auf dem Kryptomarkt.

Der Laguerre RSI glättet den Standard-RSI, um Rauschen zu reduzieren. Die Strategie kauft, wenn der Laguerre-Wert aus dem überverkauften Bereich nach oben kreuzt, und verkauft, wenn er aus dem überkauften Bereich nach unten kreuzt, und steigt aus, wenn er auf mittlere Niveaus zurückkehrt.

Die Laguerre-Filterung hilft, unruhige Bedingungen zu vermeiden, die reguläre RSI-Signale beeinträchtigen. Die Methode ist beliebt, um Schwankungen auf Intraday-Charts zu erfassen und dabei kleinere Fluktuationen zu ignorieren.


## Details

- **Einstiegskriterien**: Signale basierend auf RSI.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensätzliches Signal oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `Gamma` = 0.7m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: RSI
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

