# SpectrAnalysis Chaikin-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet den Chaikin-Oszillator, um Momentum-Wechsel zu erkennen. Der Oszillator wird aus der Akkumulations-/Distributions-Linie berechnet, die durch zwei linear gewichtete gleitende Durchschnitte geglättet wird. Wenn die Steigung des Oszillators nach oben dreht und der letzte Wert über den vorherigen Wert kreuzt, wird eine Long-Position eröffnet. Umgekehrt wird eine Short-Position eröffnet, wenn die Steigung nach unten dreht und der letzte Wert unter den vorherigen Wert kreuzt.

## Parameter

| Name | Beschreibung |
|------|--------------|
| `FastMaPeriod` | Periode des schnellen linear gewichteten gleitenden Durchschnitts im Chaikin-Oszillator. |
| `SlowMaPeriod` | Periode des langsamen linear gewichteten gleitenden Durchschnitts im Chaikin-Oszillator. |
| `BuyPosOpen` | Eröffnen von Long-Positionen aktivieren. |
| `SellPosOpen` | Eröffnen von Short-Positionen aktivieren. |
| `BuyPosClose` | Schließen von Long-Positionen bei erfüllten Bedingungen aktivieren. |
| `SellPosClose` | Schließen von Short-Positionen bei erfüllten Bedingungen aktivieren. |
| `CandleType` | Zeitrahmen der Kerzen für die Berechnung. |

## Hinweise

- Für Ein- und Ausstiege werden Marktorders verwendet.
- Die Strategie setzt keine Stop-Loss- oder Take-Profit-Orders.
- Es wird nur die C#-Version bereitgestellt; eine Python-Implementierung ist nicht enthalten.
