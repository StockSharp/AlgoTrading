# PZ Parabolic SAR EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert den *PZ Parabolic SAR*-Experten. Sie verwendet zwei Parabolic SAR-Indikatoren mit unterschiedlichen Schritt- und Maximalbeschleunigungseinstellungen. Der "Trade"-SAR erkennt die Trendrichtung für Einstiege, während der "Stop"-SAR dem Preis enger folgt und Ausstiege auslöst, wenn der Trend sich umkehrt.

Die Risikosteuerung erfolgt über den Average True Range (ATR). Beim Öffnen einer Position wird ein anfänglicher ATR-basierter Stop gesetzt. Optional kann ein ATR-basierter Trailing-Stop den Stop enger ziehen, wenn sich der Preis zugunsten des Trades bewegt. Die Strategie unterstützt auch Teilschließungen: Sobald der Gewinn die anfängliche Stop-Distanz überschreitet, wird die Hälfte der Position geschlossen und der Stop auf Break-Even verschoben.

Die Strategie funktioniert in Long- und Short-Richtung und operiert nur auf abgeschlossenen Kerzen. Es werden Marktorders ohne tatsächliche Stop-Orders verwendet.

## Details

- **Einstiegskriterien**: Preis über/unter dem Trade-SAR und dem Stop-SAR in dieselbe Richtung.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Stop-SAR kreuzt den Preis oder ATR-Trailing-Stop wird getroffen.
- **Stops**: ATR-basierter Stop mit optionalem Trailing und Break-Even.
- **Standardwerte**:
  - `TradeStep` = 0.002
  - `TradeMax` = 0.2
  - `StopStep` = 0.004
  - `StopMax` = 0.4
  - `AtrPeriod` = 30
  - `AtrMultiplier` = 2.5
  - `UseTrailing` = false
  - `TrailingAtrPeriod` = 30
  - `TrailingAtrMultiplier` = 1.75
  - `PartialClosing` = true
  - `PercentageToClose` = 0.5
  - `BreakEven` = true
  - `LotSize` = 0.1
  - `CandleType` = TimeFrame(5m)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Parabolic SAR, ATR
  - Stops: ATR, Trailing
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
