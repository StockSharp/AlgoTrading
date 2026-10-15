# OverHedgeV2 Grid-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie repliziert den MetaTrader OverHedge V2-Expertenberater auf der StockSharp High-Level-API. Sie baut ein gehedgtes Grid auf, indem sie der Richtung eines schnellen und eines langsamen EMA folgt, und wechselt dann zwischen Long- und Short-Orders innerhalb eines dynamischen Tunnels. Positionen werden gemäß einer geometrischen Lot-Progression hinzugefügt und der gesamte Korb wird liquidiert, sobald der aggregierte unrealisierte Gewinn das konfigurierte Ziel erreicht.

## Handelslogik

- **Trend-Filter:** Ein 8-Perioden-EMA muss mindestens `MinDistancePips` vom 21-Perioden-EMA abweichen. Der Filter entscheidet die Richtung des ersten Trades in jedem Zyklus.
- **Grid-Tunnel:** Die Tunnelbreite entspricht dem aktuellen Spread multipliziert mit zwei plus `TunnelWidthPips` in Preiseinheiten umgerechnet. Sie definiert den Trigger der Gegenseite, sobald der Zyklus beginnt.
- **Order-Alternation:** Die ersten drei Positionen werden in Trendrichtung eröffnet. Danach wechselt der Algorithmus die Seite, um die Exposition zu hedgen, wobei dieselben Tunnel-Anker als Referenz verwendet werden.
- **Lot-Eskalation:** Jede nachfolgende Order multipliziert das vorherige Volumen mit `BaseMultiplier`, beginnend bei `StartVolume`. Die Größe wird auf die Volumenbeschränkungen des Instruments ausgerichtet.
- **Zyklusausstieg:** Wenn der unrealisierte Nettoge­winn pro Instrument-Lot über `MinProfitTargetPips` liegt und der gesamte Korbgewinn `ProfitTargetPips` übersteigt, schließt die Strategie alle offenen Positionen und setzt den Zustand zurück.
- **Manuelles Herunterfahren:** Das Setzen von `ShutdownGrid` auf `true` schließt verbleibende Positionen und verhindert neue Orders, bis es umgeschaltet wird.

## Einstiegsbedingungen

### Long-Einstiege
- Trend-Filter zeigt Aufwärtstrend an (`EMA_short - EMA_long > MinDistancePips`).
- Ask-Preis ist größer oder gleich dem aktuellen Kaufanker.
- Die Strategie ist nicht im Shutdown-Modus und der Korb hat sein Gewinnziel nicht erreicht.

### Short-Einstiege
- Trend-Filter zeigt Abwärtstrend an (`EMA_long - EMA_short > MinDistancePips`).
- Ask-Preis ist kleiner oder gleich dem aktuellen Verkaufsanker.
- Shutdown-Flag ist falsch und das Korbgewinnziel ist noch nicht erreicht.

## Exit-Management

- **Gewinn-Exit:** Wenn der unrealisierte Korbgewinn `ProfitTargetPips` erfüllt, wobei jede offene Seite mindestens `MinProfitTargetPips` pro Lot gewinnt, werden alle Positionen zum Marktpreis geschlossen.
- **Notfall-Exit:** Das Setzen von `ShutdownGrid` auf `true` schließt sofort jede offene Exposition.

## Indikatoren und Daten

- 8-Perioden-EMA (schnell) und 21-Perioden-EMA (langsam), berechnet auf der konfigurierten Kerzenreihe.
- Level-1-Abonnement wird verwendet, um das beste Bid/Ask zu verfolgen, um den Tunnel zu erstellen und Einstiegsbedingungen mit Echtzeit-Spreads zu vergleichen.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `StartVolume` | Anfangsvolumen der ersten Order in einem Zyklus. |
| `BaseMultiplier` | Geometrischer Multiplikator auf das Volumen jeder nachfolgenden Order angewendet. |
| `TunnelWidthPips` | Zusätzliche Tunnelbreite in Pips, die zum doppelten aktuellen Spread addiert wird. |
| `ProfitTargetPips` | Korbgewinnziel gemessen in Pips, umgerechnet in Preisabstand. |
| `MinProfitTargetPips` | Minimale günstige Bewegung pro Seite, bevor der Korb schließen kann. |
| `ShortEmaPeriod` | Periode des schnellen EMA zur Richtungsbestätigung. |
| `LongEmaPeriod` | Periode des langsamen EMA zur Richtungsbestätigung. |
| `MinDistancePips` | Minimale EMA-Trennung erforderlich, um einen Trend zu erklären. |
| `CandleType` | Zeitrahmen der Kerzen, die die EMAs und die Trading-Schleife speisen. |
| `ShutdownGrid` | Boolescher Schalter, der Liquidation erzwingt und neue Trades blockiert. |

## Praktische Hinweise

- Die Standard-Kerzenperiode beträgt eine Stunde; passen Sie sie an den im ursprünglichen EA verwendeten Zeitrahmen an.
- Die Strategie verlässt sich auf Beste-Bid/Ask-Daten; Level-1-Kurse beim Live-Trading oder Backtesting bereitstellen.
- Da StockSharp eine Nettoposition pro Instrument hält, reduzieren oder drehen wechselnde Käufe und Verkäufe die Nettoexposition, anstatt unabhängige gehedgte Tickets zu halten, aber die Korblogik ahmt die beabsichtigte Gewinnerfassung dennoch nach.
- Immer instrumentspezifische Volumenschritte und Tick-Größen überprüfen, damit der generierte Tunnel und die Lot-Skalierung zum gehandelten Markt passen.
