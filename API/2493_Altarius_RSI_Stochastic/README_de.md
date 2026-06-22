# Altarius RSI Stochastic-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Altarius RSI Stochastic-Strategie ist eine direkte Konvertierung des MetaTrader 5 Expert Advisors "Altarius RSI Stohastic" in StockSharp's High-Level-API. Das System synchronisiert zwei Stochastic-Oszillatoren mit einem schnellen 3-Perioden-RSI, um kurzlebige Umkehrungen zu erfassen, die auftreten, wenn der Momentum komprimiert und dann wieder expandiert. Die StockSharp-Implementierung bewahrt die ursprüngliche Ein- und Ausstiegslogik und fügt moderne Annehmlichkeiten wie Strategieparameter, automatisches Risikomanagement und adaptives Positionssizing hinzu.

## Funktionsweise
- **Primärer Stochastic (15/8/8):** Dient als Trendfilter. Long-Positionen erfordern, dass die %K-Linie unter 50 liegt und die %D-Linie von unten kreuzt, was aufwärts gerichteten Momentum in einer neutralen bis überverkauften Zone signalisiert. Short-Positionen erfordern die Spiegelbedingung oberhalb von 55.
- **Sekundärer Stochastic (10/3/3):** Misst, wie stark %K von %D abweicht. Ein minimaler absoluter Abstand von 5 Punkten ist erforderlich, um den Momentum vor dem Einstieg zu validieren.
- **RSI (Periode 3):** Kontrolliert die Ausstiege. Long-Positionen werden geschlossen, wenn der RSI 60 überschreitet und das primäre %D von über 70 nach unten dreht. Short-Positionen werden beendet, wenn der RSI unter 40 fällt und das primäre %D von unter 30 nach oben dreht.
- **Drawdown-Schutz:** Wenn der schwebende PnL unter das konfigurierbare Risikomultiplikator des Kontokapitals fällt, liquidiert die Strategie sofort die offene Position – ähnlich wie der Notfall-Stop im Originalcode.
- **Adaptives Sizing:** Das anfängliche Volumen wird aus dem Portfoliokapital multipliziert mit dem `MaximumRisk`-Faktor und dividiert durch 1000 abgeleitet, entsprechend dem MT5-Ansatz. Aufeinanderfolgende Verlustgeschäfte reduzieren die Positionsgröße gemäß dem `DecreaseFactor`, unter Beachtung eines Mindesthandelsvolumens.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Zeitrahmen für Kerzenabonnements. | 5-Minuten-Zeitrahmen |
| `BaseVolume` | Ersatzvolumen, wenn keine Portfolioinformationen verfügbar sind. | 0.1 |
| `MinimumVolume` | Mindestvolumen nach allen Berechnungen. | 0.1 |
| `MaximumRisk` | Risikomultiplikator, der auf den Portfoliowert für Sizing und Drawdown-Ausstieg angewendet wird. | 0.1 |
| `DecreaseFactor` | Divisor, der das Volumen nach aufeinanderfolgenden Verlustgeschäften reduziert. | 3 |
| `PrimaryStochasticLength` | Lookback-Periode für die primäre Stochastic-%K-Linie. | 15 |
| `PrimaryStochasticKPeriod` | Glättung für die primäre %K-Linie. | 8 |
| `PrimaryStochasticDPeriod` | Periode für die primäre %D-Signallinie. | 8 |
| `SecondaryStochasticLength` | Lookback-Periode für den Bestätigungs-Stochastic. | 10 |
| `SecondaryStochasticKPeriod` | Glättung für die sekundäre %K-Linie. | 3 |
| `SecondaryStochasticDPeriod` | Periode für die sekundäre %D-Linie. | 3 |
| `DifferenceThreshold` | Mindestabstand zwischen sekundärem %K und %D zur Zulassung von Einstiegen. | 5 |
| `PrimaryBuyLimit` | Maximaler primärer %K-Wert vor dem Öffnen einer Long-Position. | 50 |
| `PrimarySellLimit` | Minimaler primärer %K-Wert vor dem Öffnen einer Short-Position. | 55 |
| `PrimaryExitUpper` | Primärer %D-Schwellenwert, der vor dem Schließen von Longs überschritten werden muss. | 70 |
| `PrimaryExitLower` | Primärer %D-Schwellenwert, der vor dem Schließen von Shorts unterschritten werden muss. | 30 |
| `RsiPeriod` | RSI-Lookback-Länge. | 3 |
| `LongExitRsi` | RSI-Niveau, das Long-Ausstiege bestätigt. | 60 |
| `ShortExitRsi` | RSI-Niveau, das Short-Ausstiege bestätigt. | 40 |

## Handelsregeln
1. **Einstiegskriterien**
   - **Long:** Primäres %K > primäres %D, primäres %K < `PrimaryBuyLimit`, und |sekundäres %K − sekundäres %D| > `DifferenceThreshold`, während die Strategie flat ist.
   - **Short:** Primäres %K < primäres %D, primäres %K > `PrimarySellLimit`, und |sekundäres %K − sekundäres %D| > `DifferenceThreshold`, während die Strategie flat ist.
2. **Ausstiegskriterien**
   - **Long-Ausstieg:** RSI > `LongExitRsi`, primäres %D > `PrimaryExitUpper`, und der aktuelle %D-Wert ist niedriger als der der vorherigen Kerze.
   - **Short-Ausstieg:** RSI < `ShortExitRsi`, primäres %D < `PrimaryExitLower`, und der aktuelle %D-Wert ist höher als der der vorherigen Kerze.
   - **Risikoausstieg:** Wenn der schwebende Verlust `MaximumRisk × Portfolio.CurrentValue` überschreitet.

## Risikomanagement
- Die Strategie ruft automatisch `StartProtection()` auf, um StockSharp's integrierte Positionsschutzdienste zu aktivieren.
- Die Positionsgröße wird reduziert, wenn `_lossStreak` mehr als ein aufeinanderfolgendes Verlustgeschäft überschreitet, und ahmt die MT5-`DecreaseFactor`-Logik nach.
- `MinimumVolume` verhindert, dass die Positionsgröße unter die Mindesttickanforderungen der Börse fällt.

## Hinweise
- Die Strategie setzt ein hedging-fähiges Portfolio voraus, genau wie der ursprüngliche EA.
- Passen Sie den `CandleType`-Parameter an den Zeitrahmen an, den Sie in MetaTrader verwendet hätten (M1, M5 usw.).
- Kombinieren Sie dieses Modul mit StockSharp Designer oder dem Backtester-Projekt in diesem Repository, um die Performance mit Ihren eigenen Daten zu validieren.
