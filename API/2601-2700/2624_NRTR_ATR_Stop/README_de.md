# NRTR ATR Stop Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die NRTR ATR Stop Strategie reproduziert das Verhalten des MetaTrader-Experten `Exp_NRTR_ATR_STOP` mit StockSharps High-Level-API. Sie verfolgt die Non-Repainting Trailing Reverse (NRTR)-Niveaus, die aus dem Average True Range (ATR) aufgebaut werden. Wenn der Preis den entgegengesetzten Trailing Stop kreuzt, kehrt sich der Trend um, was einen frischen Markteinstieg generiert und gleichzeitig jede offene Position in der vorherigen Richtung schließt.

## Indikatorlogik
* Ein einziger **Average True Range** (`AtrPeriod`) wird aus der abonnierten Kerzenserie berechnet. Der ATR-Wert wird mit dem `Coefficient` multipliziert, um den Abstand zwischen Preis und aktuellem Stop-Niveau zu erzeugen.
* Zwei dynamische Stop-Linien werden aufrechterhalten:
  * `upper stop` schützt Long-Positionen. Er trails unter dem Preis, während der Trend bullisch ist.
  * `lower stop` schützt Short-Positionen. Er trails über dem Preis, während der Trend bärisch ist.
* Wenn der Preis jenseits des entgegengesetzten Stops schließt, kehrt sich der Trend sofort um. Der Stop auf der neuen Seite wird unter Verwendung des Extremums der vorherigen Kerze minus/plus der ATR-Distanz initialisiert.
* Der ursprüngliche Experte verzögert die Ausführung, indem er den Indikatorpuffer `SignalBar` Kerzen zurückliest. Die Strategie spiegelt dieses Verhalten durch eine interne Warteschlange wider: Jede abgeschlossene Kerze schiebt ihr Signal in die Warteschlange, und die Engine handelt erst, wenn die Warteschlangenlänge `SignalBar` überschreitet.

## Handelsregeln
1. **Kaufsignal** – der berechnete Trend wechselt von neutral/bärisch zu bullisch. Die Strategie schließt optional jedes Short-Engagement und öffnet eine frische Long-Position mit einem einzelnen Marktauftrag, dessen Volumen der erforderlichen Ausstiegsgröße plus dem konfigurierten `Volume` für den neuen Long-Einstieg entspricht.
2. **Verkaufssignal** – der Trend wechselt von neutral/bullisch zu bärisch. Die Strategie schließt optional jedes Long-Engagement und öffnet eine neue Short-Position auf gleiche Weise.
3. Die Eigenschaften `EnableLongEntry`, `EnableShortEntry`, `EnableLongExit` und `EnableShortExit` ermöglichen eine präzise Kontrolle darüber, welche Aktionen bei Erscheinen eines Signals ausgeführt werden.
4. Signale werden nur bei abgeschlossenen Kerzen verarbeitet, während die Strategie online ist und zum Handel berechtigt.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `AtrPeriod` | Anzahl der Kerzen für die ATR-Berechnung. |
| `Coefficient` | Multiplikator für den ATR-Wert beim Aufbau der Trailing Stops. |
| `SignalBar` | Anzahl vollständig geschlossener Kerzen, die vor dem Handeln auf ein gespeichertes Signal gewartet werden soll. Auf `0` setzen, um sofort auf der aktuellen Kerze zu handeln. |
| `CandleType` | Zeitrahmen der eingehenden Kerzen. |
| `EnableLongEntry` | Long-Positionen bei Kaufsignalen öffnen erlauben. |
| `EnableShortEntry` | Short-Positionen bei Verkaufssignalen öffnen erlauben. |
| `EnableLongExit` | Long-Positionen schließen erlauben, wenn ein Verkaufssignal auftritt. |
| `EnableShortExit` | Short-Positionen schließen erlauben, wenn ein Kaufsignal auftritt. |

## Hinweise
* Die Strategie basiert ausschließlich auf abgeschlossenen Kerzen; Intrabar-Ticks werden ignoriert.
* Aufträge werden mit `BuyMarket`/`SellMarket` gesendet, was Positionsschließung und frischen Einstieg zur Vereinfachung in einem einzelnen Marktauftrag kombiniert.
* Stellen Sie sicher, dass die `Volume`-Eigenschaft auf einen positiven Wert gesetzt ist, bevor Sie Live-Handel oder Backtesting starten.
