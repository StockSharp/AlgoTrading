# Four Hour Swing-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Four Hour Swing-Strategie** portiert den MetaTrader-Expert-Advisor „4H swing" auf die hochrangige StockSharp-API. Das ursprüngliche System kombiniert Trendfolge und Oszillatorbestätigungen aus höheren Zeitrahmen. Diese C#-Version abonniert drei Zeitrahmen (Einstieg, Bestätigung und Makro-Filter) und recreiert den Indikator-Stack mit StockSharp-Komponenten.

## Handelslogik
- Der Haupt-Trendfilter verwendet drei exponentielle gleitende Durchschnitte, berechnet auf dem typischen Preis der Einstiegskerzen. Ein Long-Setup erfordert `Fast EMA > Medium EMA > Slow EMA`; ein Short-Setup spiegelt die Bedingung.
- Stochastic-Oszillatorwerte werden auf dem höheren Bestätigungszeitrahmen ausgewertet. Die %K-Linie muss für Longs über %D und für Shorts darunter bleiben.
- Momentum wird aus denselben Bestätigungskerzen abgetastet und in das MetaTrader-Verhältnis um 100 umgewandelt. Ein Trade ist nur erlaubt, wenn mindestens eine der letzten drei Momentum-Messwerte weiter als der konfigurierte Schwellenwert entfernt ist.
- Monatliche (oder benutzerdefinierte) MACD-Werte liefern den Makro-Richtungsfilter. Ein Kauf erfordert die MACD-Linie über ihrem Signal, während ein Verkauf die entgegengesetzte Beziehung prüft.

Eine Position wird auf der nächsten Basiskerze eröffnet, sobald alle Bestätigungen übereinstimmen und das Konto flach oder in entgegengesetzter Richtung positioniert ist (in diesem Fall schließt und kehrt die Marktorder um).

## Risikomanagement
- Feste Stop-Loss- und Take-Profit-Abstände (in Pips) werden sofort nach dem Einstieg angewendet.
- Ein optionaler Trailing-Stop folgt dem extremen Preis nach dem Einstieg.
- Break-Even-Schutz kann den Stop auf den Einstiegspreis plus einen Offset verschieben, sobald die konfigurierte Auslösedistanz erreicht ist.
- Ein optionaler MACD-Ausstieg schließt offene Trades, wenn der Makro-Filter kippt.

## Parameter
| Name | Beschreibung | Standardwert |
| --- | --- | --- |
| `TradeVolume` | Standard-Marktordervolumen. | `0.01` |
| `CandleType` | Einstiegskerzentyp (z. B. 4-Stunden-Kerzen). | `4H` |
| `SignalCandleType` | Bestätigungskerzentyp für Stochastic und Momentum. | `7D` (wöchentlich) |
| `MacdCandleType` | Makro-Filter-Kerzentyp. | `30D` |
| `FastEmaPeriod`, `MediumEmaPeriod`, `SlowEmaPeriod` | EMA-Längen auf dem typischen Preis. | `4`, `14`, `50` |
| `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSmoothPeriod` | Stochastic-Oszillator-Einstellungen. | `13`, `5`, `5` |
| `MomentumPeriod` | Rückblickperiode des Momentum-Indikators. | `14` |
| `MomentumThreshold` | Mindestabstand von 100 zur Validierung des Momentums. | `0.3` |
| `StopLossPips`, `TakeProfitPips` | Schutzorders in Pips. | `20`, `50` |
| `TrailingStopPips` | Trailing-Stop-Abstand in Pips. Auf null setzen zum Deaktivieren. | `40` |
| `UseBreakEven` | Aktiviert den Break-Even-Schutz. | `true` |
| `BreakEvenTriggerPips`, `BreakEvenOffsetPips` | Auslöser und Offset für die Break-Even-Verschiebung. | `30`, `30` |
| `UseMacdExit` | Positionen schließen, wenn der Makro-MACD kippt. | `false` |

## Hinweise
- Die Geldmanagement-Funktionen (Eigenkapital-Stops, Währungsziele) des ursprünglichen Experten sind absichtlich weggelassen, um die Implementierung kompakt zu halten.
- Die Strategie verarbeitet nur abgeschlossene Kerzen, entsprechend der bar-für-bar-Auswertung von MetaTrader.
- Standard-Zeitrahmen reproduzieren das übliche 4-Stunden-Setup (wöchentliche Bestätigung und monatlicher Filter), aber jeder `DataType`-Parameter kann für alternative Perioden neu konfiguriert werden.
