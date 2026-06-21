# MPM-Momentum-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine vereinfachte Konvertierung des ursprünglichen MQL-Experten `mpm-1_8.mq4`.
Sie wartet auf eine Folge progressiver Kerzen und eröffnet dann eine Position in
dieselbe Richtung. Der Average True Range wird verwendet, um die Kerzengröße zu bewerten
und Stops zu verfolgen.

## Parameter

| Name | Beschreibung |
| ---- | ------------ |
| `ProgressiveCandles` | Anzahl aufeinanderfolgender Kerzen, die zum Auslösen eines Trades erforderlich sind. |
| `ProgressiveSize` | Minimale Kerzenkörpergröße relativ zum ATR, um als progressiv zu gelten. |
| `StopRatio` | ATR-Anteil für das Trailing des Stop-Niveaus. |
| `AtrPeriod` | Periode des Average-True-Range-Indikators. |
| `CandleType` | Von der Strategie verwendeter Kerzentyp. |
| `ProfitPerLot` | Gewinnziel pro Lot. |
| `BreakEvenPerLot` | Gewinn, der zum Ausstieg beim Breakeven erforderlich ist. |
| `LossPerLot` | Maximal tolerierter Verlust pro Lot. |

## Logik

1. Bei jeder abgeschlossenen Kerze wird die Körpergröße mit dem ATR verglichen.
2. Ein bullischer oder bärischer Zähler wird erhöht, wenn der Körper den
   `ProgressiveSize`-Schwellenwert überschreitet.
3. Nach `ProgressiveCandles` in eine Richtung wird eine Market-Order gesendet.
4. Das Stop-Niveau wird um `StopRatio` des ATR nachgezogen.
5. Positionen werden geschlossen, wenn der Stop erreicht wird oder wenn Gewinn-/Verlustziele
   erreicht werden.
