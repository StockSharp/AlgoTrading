# MBKAsctrend3-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die MBKAsctrend3-Strategie verwendet drei Williams %R-Oszillatoren mit unterschiedlichen Perioden. Ihre gewichtete Kombination definiert den Markttrend. Eine Long-Position wird eröffnet, wenn der gewichtete Wert über einen oberen Schwellenwert kreuzt und der langfristige Oszillator ebenfalls hoch ist. Eine Short-Position wird eröffnet, wenn die Werte unter ihre unteren Schwellenwerte fallen. Positionen werden durch konfigurierbare Stop-Loss- und Take-Profit-Niveaus in Punkten geschützt.

## Details
- **Einstiegskriterien**:
  - **Long**: Weighted WPR > 67+Swing und long WPR > 50-AverageSwing.
  - **Short**: Weighted WPR < 33-Swing und long WPR < 50+AverageSwing.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Entgegengesetztes Signal oder Schutzniveaus.
- **Stops**: Absoluter Stop-Loss und Take-Profit.
- **Filter**: Keine.

## Parameter
- `WprLength1`, `WprLength2`, `WprLength3` – Perioden der drei Williams %R-Indikatoren.
- `Swing` – Verschiebung der oberen/unteren Schwellenwerte.
- `AverageSwing` – zusätzliche Verschiebung basierend auf dem langfristigen Oszillator.
- `Weight1`, `Weight2`, `Weight3` – Gewichte für jeden Indikator.
- `StopLoss`, `TakeProfit` – Schutzniveaus in Punkten.
- `CandleType` – Zeitrahmen der Kerzen, Standard 4 Stunden.
