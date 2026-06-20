# Arsi Vwap Atr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Adaptive RSI-Strategie, bei der die Überkauft- und Überverkauft-Niveaus je nach ATR oder der Abweichung vom VWAP expandieren oder kontrahieren. Positionen werden bei RSI-Kreuzungen der adaptiven Niveaus eröffnet und geschlossen, wenn der RSI in die Mittelzone zurückkehrt.

## Details

- **Einstiegskriterien**:
  - Long: `RSI` kreuzt über die adaptive Überverkauft-Linie
  - Short: `RSI` kreuzt unter die adaptive Überkauft-Linie
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - RSI kreuzt zurück durch 50 oder die entgegengesetzte adaptive Linie
- **Stops**: Prozentbasiert mit `StopLossPercent` und `RiskReward`
- **Standardwerte**:
  - `RsiLength` = 14
  - `BaseK` = 1m
  - `RiskPercent` = 2m
  - `StopLossPercent` = 2.5m
  - `RiskReward` = 2m
  - `SourceOb` = ATR
  - `SourceOs` = ATR
  - `AtrLengthOb` = 14
  - `AtrLengthOs` = 14
  - `ObMultiplier` = 10m
  - `OsMultiplier` = 10m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: RSI, ATR, VWAP
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
