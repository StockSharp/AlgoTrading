# Range-Follower-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Range Follower-Strategie reproduziert den MetaTrader 5-Expertenberater „Range Follower“ unter Verwendung des StockSharp-High-Level-API. Es überwacht die Preisspanne des aktuellen Tages im Verhältnis zu einem täglichen Average True Range (ATR)-Benchmark und eröffnet einen einzelnen Breakout-Trade, wenn sich der Preis weit genug vom Sitzungshoch oder -tief entfernt. Die Konvertierung behält den ursprünglichen Ansatz bei, den ATR in einen Triggeranteil und einen Restanteil aufzuteilen, der zur Take-Profit-Distanz wird.

## Handelslogik
1. **Basislinie der täglichen Volatilität**
   - Ein 20-Perioden-ATR, berechnet auf täglichen Kerzen, liefert den Basisbereich für den aktuellen Handelstag.
   - Der ATR-Wert wird von `TriggerPercent` in zwei Segmente aufgeteilt: die Triggerdistanz, die vor dem Eintritt überschritten werden muss, und die verbleibende Distanz, die als Gewinnziel verwendet wird.
2. **Reichweitenverfolgung**
   - Die Strategie zeichnet kontinuierlich das aktuelle Sitzungshoch und -tief der aktiven Tageskerze auf.
   - Level-1-Updates liefern die neuesten besten Geld- und Briefkurse, die zur Messung des Abstands zwischen den aktuellen Kursen und den Extremwerten der Sitzung verwendet werden.
3. **Einzeleintritt pro Tag**
   - Wenn das beste Gebot mehr als die Triggerdistanz über dem Sitzungstief liegt und noch kein Handel eröffnet wurde, kauft die Strategie zum Marktwert.
   - Wenn der beste Brief mehr als die Triggerdistanz unter dem Sitzungshoch liegt und noch kein Handel eröffnet wurde, wird die Strategie zum Marktpreis verkauft.
   - Es ist nur ein Handel pro Tag erlaubt; Das Flag wird zurückgesetzt, wenn eine neue Sitzung beginnt.
4. **Stop-Loss und Take-Profit**
   - Bei Long-Positionen wird der Stop-Loss eine Triggerdistanz unter dem Einstiegspreis und der Take-Profit eine Restdistanz darüber platziert.
   - Bei Short-Positionen liegt der Stop-Loss eine Triggerdistanz über dem Einstiegspreis und der Take-Profit eine Restdistanz darunter.
   - Die Preisüberwachung erfolgt sowohl für Level1-Ticks als auch für Kerzenaktualisierungen, um Positionen zu schließen, sobald ein Level durchbrochen wird.
5. **Tägliches Sitzungs-Reset**
   - Bei der ersten Kerze eines neuen Handelstages schließt die Strategie alle offenen Positionen, löscht den internen Status und lädt die ATR-Basislinie neu.
   - Wenn die aktuelle Tagesspanne bei Beginn der Sitzung bereits die Triggerdistanz überschreitet, wird der Handel für den Rest des Tages übersprungen, um die Sicherheitsüberprüfung des ursprünglichen EA nachzuahmen.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `CandleType` | 15-Minuten-Kerzen | Arbeitszeitraum, der zur Erkennung von Sitzungsgrenzen verwendet wird. |
| `TriggerPercent` | 60 | Prozentsatz des täglichen ATR, der als Breakout-Trigger-Distanz verwendet wird. Muss zwischen 10 und 90 bleiben. |
| `Volume` | 0,1 | Market-Order-Volumen für Long- und Short-Einstiege. |

## Risikomanagement
- Stopps und Ziele werden von der gleichen ATR-Basislinie abgeleitet, sodass das Chance-Risiko-Verhältnis immer gleich `(100 - TriggerPercent) : TriggerPercent` ist.
- Die Strategie registriert jeweils eine einzelne Position und liquidiert diese sofort, wenn der Stop oder das Ziel erreicht wird, wodurch mehrere überlappende Trades vermieden werden.
- `StartProtection()` aktiviert die schützende Infrastruktur von StockSharp und ermöglicht es externen Komponenten, bei Bedarf Trailing Stops oder Portfolio Guards anzufügen.

## Implementierungshinweise
- Tägliche ATR-Werte werden durch ein dediziertes tägliches Kerzenabonnement und den `AverageTrueRange`-Indikator erzeugt, der durch den High-Level-API gebunden ist.
- Daten der Ebene 1 sind erforderlich, um die tickgesteuerten Entscheidungen von EA widerzuspiegeln. Der beste Geld- und der beste Briefkurs bestimmen sowohl die Einstiegs- als auch die Ausstiegsprüfung.
- Tägliche Sitzungsgrenzen werden von den Arbeitszeitrahmen-Kerzen abgeleitet, um sicherzustellen, dass jeder in StockSharp verwendete Handelskalender die Strategie konsistent zurücksetzt.
- Die Konvertierung vermeidet manuelle Indikatorpuffer oder historische Schleifen und verlässt sich stattdessen auf zustandsbehaftete Felder, die durch die `Bind`-Rückrufe aktualisiert werden.
