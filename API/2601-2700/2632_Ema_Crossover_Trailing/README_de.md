# EMA Crossover Trailing-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie ist ein StockSharp-Port des MQL5 Expert Advisors **"Intersection 2 iMA"**. Sie operiert mit zwei exponentiellen gleitenden Durchschnitten (EMAs) und reagiert auf Kreuzungen, die auf vollständig gebildeten Kerzen auftreten. Der ursprüngliche Experte wurde für MetaTrader 5 entwickelt und verwaltete das Handelsvolumen dynamisch; in dieser Konvertierung wird die Ordergröße durch einen konfigurierbaren Parameter gesteuert, während die Kreuzungs- und Trailing-Logik erhalten bleibt.

## Handelslogik
1. **Signalgenerierung**
   - Schnelle und langsame EMAs auf der ausgewählten Kerzenserie berechnen.
   - Eine **bullische Kreuzung** (schnelle EMA kreuzt über langsame EMA) löst ein Kaufsignal aus, wenn die vorherige Kerze mit der schnellen EMA unter oder gleich der langsamen EMA schloss und die aktuellen Werte die schnelle EMA über der langsamen zeigen.
   - Eine **bärische Kreuzung** (schnelle EMA kreuzt unter langsame EMA) spiegelt die obige Regel und produziert ein Verkaufssignal.
2. **Orderausführung**
   - Wenn ein Kaufsignal erzeugt wird und keine Long-Position besteht, sendet die Strategie eine Market-Kauforder.
   - Wenn ein Verkaufssignal erzeugt wird und keine Short-Position besteht, sendet die Strategie eine Market-Verkaufsorder.
   - Wenn eine Gegenposition besteht, wird das Ordervolumen erhöht, um die bestehende Position zu schließen, bevor die neue eröffnet wird, was dem Verhalten des Quell-EA entspricht, der zuerst Gegentrades schloß.
3. **Trailing-Stop-Management**
   - Ein gestufter Trailing-Stop hält einen festen Abstand (in Preisschritten) vom günstigsten Preis.
   - Der Stop bewegt sich nur, wenn der Preis um einen benutzerdefinierten Schritt vorangeschritten ist, was ständige Orderänderungen verhindert.
   - Wenn der Preis das Trailing-Niveau verletzt, wird die Position mit einer Market-Order geschlossen.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `FastPeriod` | Länge der schnellen EMA. | 4 |
| `SlowPeriod` | Länge der langsamen EMA. | 18 |
| `TrailingStopPoints` | Abstand zwischen Marktpreis und Trailing-Stop in Preisschritten (Punkten). Ein Wert von `0` deaktiviert Trailing. | 20 |
| `TrailingStepPoints` | Minimaler Fortschritt in Preisschritten, bevor der Trailing-Stop vorgeschoben wird. | 5 |
| `CandleType` | Kerzendatenserie für Berechnungen (Zeitrahmen). | 15-Minuten-Kerzen |
| `TradeVolume` | Ordergröße für Market-Einstiege. | 1 |

## Implementierungshinweise
- Die Strategie verwendet die High-Level-API `SubscribeCandles().Bind(...)`, um Kerzendaten mit EMA-Indikatoren zu verbinden und sicherzustellen, dass keine manuelle Bufferverwaltung erforderlich ist.
- Trailing-Abstände werden berechnet, indem die konfigurierte Punktanzahl mit dem `PriceStep` des Instruments multipliziert wird, was die Ziffernanpassungslogik aus der MQL-Version repliziert.
- Trailing-Stops werden intern über Market-Exits implementiert, da StockSharp nicht den gleichen `PositionModify`-Helper wie MetaTrader bereitstellt. Das Verhalten bleibt äquivalent: sobald das Trailing-Niveau verletzt wird, wird die Position sofort beendet.
- Parameter werden über `StrategyParam<T>` bereitgestellt, sodass sie im Designer optimiert oder über die Benutzeroberfläche angepasst werden können.

## Verwendungstipps
- Den `CandleType` auf den Zeitrahmen ausrichten, der in Backtests oder im Live-Handel verwendet wird, um Indikatorwerte konsistent zu halten.
- Beim Handel mit Instrumenten mit kleinen Tick-Größen `TrailingStopPoints` und `TrailingStepPoints` entsprechend anpassen; der effektive Preisabstand entspricht *Punkte × PriceStep*.
- `TradeVolume` auf die gewünschte Kontrakt- oder Lotgröße einstellen. Die Strategie erhöht den Orderbetrag automatisch, um eine Gegenposition zu schließen, wenn ein neues Signal erscheint.

## Unterschiede zum ursprünglichen Expert Advisor
- Money-Management in MetaTrader verwendete `MoneyFixedMargin`; die StockSharp-Version stellt stattdessen einen festen Ordervolumen-Parameter bereit und überlässt das erweiterte Positionssizing der externen Konfiguration.
- Der EA bot einen unbenutzten `InpCloseHalf`-Eingabewert an. Er hatte keine Auswirkung auf den Quellcode und wurde weggelassen.
- Stop-Trailing wird intern statt durch Modifikation von Stop-Loss-Orders gehandhabt, da dies die Ausführung innerhalb von StockSharp vereinfacht und die Ausstiegslogik identisch hält.
