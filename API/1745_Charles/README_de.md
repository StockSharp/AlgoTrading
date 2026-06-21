# Charles EMA RSI Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie emuliert den Charles Expert Advisor, indem sie exponentielle gleitende Durchschnitte (EMA) mit einem RSI-Filter und einem Trailing Stop kombiniert. Sie handelt in beide Richtungen und schützt Positionen dynamisch.

Das System überwacht einen schnellen und einen langsamen EMA im gewählten Zeitrahmen. Wenn der schnelle EMA den langsamen EMA nach oben kreuzt und der RSI 55 überschreitet, eröffnet die Strategie eine Long-Position. Umgekehrt, wenn der schnelle EMA den langsamen EMA nach unten kreuzt und der RSI unter 45 fällt, wird eine Short-Position eröffnet. Nach dem Einstieg folgt ein Trailing Stop dem Preis, um Gewinne zu sichern, während ein fester Take Profit und Stop Loss durch den integrierten Positionsschutz verwaltet werden.

## Details

- **Einstiegskriterien**:
  - **Long**: `Schneller EMA` kreuzt über `langsamen EMA` und `RSI > 55`.
  - **Short**: `Schneller EMA` kreuzt unter `langsamen EMA` und `RSI < 45`.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Trailing Stop.
  - Stop Loss oder Take Profit.
- **Stops**: Verwendet integrierten Schutz mit Trailing.
- **Standardwerte**:
  - `FastPeriod` = 18
  - `SlowPeriod` = 60
  - `RsiPeriod` = 14
  - `TakeProfit` = 0.02
  - `StopLoss` = 0.008
  - `TrailStart` = 0.006
  - `TrailOffset` = 0.003
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Mehrere
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Standardmäßig 1 Stunde
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
