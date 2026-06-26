# Candle Trailing Stop-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Candle Trailing Stop**-Strategie ist eine StockSharp-Portierung des gleichnamigen MetaTrader-Expert-Advisors. Der ursprüngliche Roboter kombinierte Multi-Timeframe-Trendfilter, Momentum-Bestätigung und eine aggressive Trailing-Stop-Engine, die den Tiefs und Hochs der letzten Kerzen folgte. Die C#-Version behält denselben Workflow bei, stützt sich aber auf High-Level-StockSharp-Komponenten und macht alle wichtigen Einstellungen als Strategieparameter verfügbar.

## Kernlogik

1. **Datenabonnements**
   - Der Handelszeitrahmen treibt Einstiege und Trailing-Stop-Updates an.
   - Ein höherer Zeitrahmen liefert Bestätigung mithilfe von linear gewichteten gleitenden Durchschnitten (LWMA) und einem Momentum-Indikator.
   - Ein drittes Abonnement berechnet eine MACD-Linie auf einem langsamen Zeitrahmen (standardmäßig monatlich) zum Filtern von Trades.
2. **Trendausrichtung**
   - Trades sind nur erlaubt, wenn die Sequenzen des schnellen, mittleren und langsamen LWMA auf beiden Zeitrahmen ausgerichtet sind (bullische Sequenz für Longs, bearische für Shorts).
3. **Momentum-Gate**
   - Der Momentum-Indikator muss bei mindestens einem der letzten drei Höherzeitrahmen-Balken in der Nähe des neutralen Werts 100 liegen.
4. **MACD-Bestätigung**
   - Longs erfordern, dass die MACD-Linie über der Signallinie liegt; Shorts erfordern die umgekehrte Beziehung.
5. **Einstiegstrigger**
   - Ein Ausbruch durch den schnellen LWMA im aktuellen Zeitrahmen (Kerze schließt über/unter dem Durchschnitt nach Berühren im vorherigen Balken) initiiert neue Trades unter Einhaltung eines konfigurierbaren Positionslimits.
6. **Risiko- und Exit-Management**
   - Anfängliche Stop-Loss- und Take-Profit-Abstände werden in Pips definiert und automatisch in Preisschritte umgerechnet.
   - Stops können auf Break-Even migrieren, hinter dem Extrem der letzten Kerzen nachziehen oder auf ein klassisches Trailing mit fester Distanz zurückfallen.
   - Optionale kapitalbasierte Features spiegeln den ursprünglichen EA wider: monetäres Take-Profit, prozentuales Take-Profit, Eigenkapital-Trailing und Drawdown-Schutz.

## Parameter

| Gruppe | Name | Beschreibung | Standard |
|--------------|-------------------------|---------------------------------------------------------------------------------------------|---------|
| Handel | `Volume` | Ordergröße in Lots/Kontrakten. | `1` |
| | `MaxTrades` | Maximales aggregiertes Engagement ausgedrückt als `Volume * MaxTrades`. | `10` |
| Indikatoren | `FastCurrentLength` | Schneller LWMA im Handelszeitrahmen. | `9` |
| | `MiddleCurrentLength` | Mittlerer LWMA im Handelszeitrahmen. | `20` |
| | `SlowCurrentLength` | Langsamer LWMA im Handelszeitrahmen. | `52` |
| | `FastHigherLength` | Schneller LWMA im höheren Zeitrahmen. | `9` |
| | `MiddleHigherLength` | Mittlerer LWMA im höheren Zeitrahmen. | `20` |
| | `SlowHigherLength` | Langsamer LWMA im höheren Zeitrahmen. | `52` |
| | `MomentumPeriod` | Momentum-Periode im höheren Zeitrahmen. | `14` |
| | `MomentumBuyThreshold` | Maximale Abweichung von 100 für Long-Trades. | `0.3` |
| | `MomentumSellThreshold` | Maximale Abweichung von 100 für Short-Trades. | `0.3` |
| | `MacdFastLength` | Schnelle EMA-Länge für MACD-Bestätigung. | `12` |
| | `MacdSlowLength` | Langsame EMA-Länge für MACD-Bestätigung. | `26` |
| | `MacdSignalLength` | Signal-EMA-Länge für MACD-Bestätigung. | `9` |
| Risiko | `StopLossPips` | Stop-Loss-Distanz in Pips. | `20` |
| | `TakeProfitPips` | Take-Profit-Distanz in Pips. | `50` |
| | `UseMoveToBreakEven` | Aktiviert die Break-Even-Logik. | `true` |
| | `BreakEvenTriggerPips` | Gewinn in Pips erforderlich, bevor der Stop verschoben wird. | `30` |
| | `BreakEvenOffsetPips` | Offset beim Verschieben des Stops auf Break-Even. | `30` |
| | `UseCandleTrail` | Wählen zwischen kerzenbasiertem Trailing (`true`) oder klassischem Trailing (`false`). | `true` |
| | `CandleTrailLength` | Anzahl der geschlossenen Kerzen zur Berechnung der Trailing-Extreme. | `3` |
| | `PadAmountPips` | Zusätzlicher Puffer unterhalb/oberhalb des Trailing-Extrems. | `10` |
| | `TrailTriggerPips` | Gewinn erforderlich, bevor das klassische Trailing aktiviert. | `40` |
| | `TrailAmountPips` | Vom klassischen Trailing gehaltene Distanz. | `40` |
| Eigenkapitalregeln | `UseMoneyTakeProfit` | Alle Positionen schließen, wenn der schwebende Gewinn das monetäre Ziel überschreitet. | `false` |
| | `MoneyTakeProfit` | Monetäres Gewinnziel. | `40` |
| | `UsePercentTakeProfit` | Alle Positionen schließen, wenn der schwebende Gewinn das Prozentziel überschreitet. | `false` |
| | `PercentTakeProfit` | Prozentsatz des Anfangskapitals als Gewinnziel. | `10` |
| | `EnableMoneyTrailing` | Aktiviert Trailing des schwebenden Gewinns nach einem Schwellenwert. | `true` |
| | `MoneyTrailTarget` | Gewinnniveau, das die monetäre Trailing-Logik einschaltet. | `40` |
| | `MoneyTrailStop` | Maximaler erlaubter Rückgang nach Erreichen des Ziels. | `10` |
| | `UseEquityStop` | Aktiviert Eigenkapital-Drawdown-Schutz. | `true` |
| | `EquityRiskPercent` | Maximaler Drawdown vom Eigenkapitalhöchststand vor erzwungener Flat-Position. | `1` |
| Daten | `CurrentCandleType` | Handelszeitrahmen. | `5m` |
| | `HigherCandleType` | Höherer Zeitrahmen für Filter. | `30m` |
| | `MacdCandleType` | Zeitrahmen für MACD-Bestätigung (standardmäßig monatlich). | `30d` |

## Hinweise und Annahmen

- Pips werden mit der Instrument-Tick-Größe in Preisschritte umgerechnet. Bei Symbolen, bei denen ein Pip von einem Tick abweicht, müssen möglicherweise die Standard-Pip-Abstände angepasst werden.
- Monetäre Features basieren auf dem nicht realisierten Gewinn, angenähert als `(Schlusskurs - Durchschnittspreis) * Position`. Swap- und Provisionsanpassungen werden nicht simuliert.
- Die Strategie verwendet Market-Orders für Einstiege und Ausstiege. Anfängliche Take-Profit-Orders werden registriert, sobald ein Trade geöffnet wird, während das Stop-Loss-Management intern gehandhabt wird und durch Market-Orders schließt, wenn das berechnete Niveau überschritten wird.
- Alle Code-Kommentare sind gemäß den Projektrichtlinien auf Englisch verfasst.
