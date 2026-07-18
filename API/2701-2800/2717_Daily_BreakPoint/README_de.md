# Tages-Bruchpunkt-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Tages-Bruchpunkt-Strategie** ist eine StockSharp-Portierung des MetaTrader 5-Experten «Daily BreakPoint» (Build 19498). Der Algorithmus überwacht den Abstand zwischen dem aktuellen Preis und der täglichen Eröffnung. Wenn die Bewegung von der täglichen Eröffnung einen konfigurierbaren Schwellenwert überschreitet und die vorherige Kerze strenge Körpergrößenanforderungen erfüllt, tritt die Strategie in Richtung des Ausbruchs ein oder dreht die bestehende Exposition, abhängig vom `CloseBySignal`-Flag.

Die Strategie arbeitet gleichzeitig mit zwei Datenstreams:

1. Intraday-Kerzen, definiert durch den `CandleType`-Parameter, für die Signalgenerierung.
2. Tageskerzen, um den letzten Sitzungseröffnungspreis zu verfolgen.

## Handelslogik
1. Wenn eine neue Intraday-Kerze schließt, liest die Strategie den letzten täglichen Eröffnungspreis und berechnet die Ausbruchsniveaus mit `BreakPointPips` (in absolute Preise über die Tick-Größe des Instruments umgerechnet).
2. Die Körpergröße der kürzlich geschlossenen Kerze muss innerhalb des Bereichs `[LastBarSizeMinPips, LastBarSizeMaxPips]` liegen.
3. **Bullisches Setup**
   - Die Kerze muss über ihrer Eröffnung schließen (`Close > Open`).
   - Der Schlusskurs muss mindestens `BreakPointPips` über der täglichen Eröffnung liegen.
   - Der Ausbruchspreis (tägliche Eröffnung + Bruchpunkt) muss innerhalb des Kerzenkörpers liegen.
   - Wenn `CloseBySignal = false`, öffnet die Strategie eine Long-Position. Andernfalls schließt sie jede offene Long-Exposition und eröffnet eine Short-Position.
4. **Bärisches Setup** spiegelt den bullischen Fall: eine bärische Kerze, deren Schlusskurs mindestens `BreakPointPips` unter der täglichen Eröffnung liegt und deren Körper das Ausbruchsniveau enthält, löst entweder einen Short-Einstieg (`CloseBySignal = false`) oder eine Umkehr in eine Long-Position (`CloseBySignal = true`) aus.
5. Orders werden als Marktorders mit dem konfigurierten `OrderVolume` gesendet. Die Positionsgröße ist kumulativ, sodass mehrere Signale die Position in beide Richtungen skalieren können.

## Risikomanagement
- **Stop-Loss / Take-Profit**: Optionale feste Ziele definiert in Pips (`StopLossPips`, `TakeProfitPips`). Wenn auf null gesetzt, ist das entsprechende Level deaktiviert. Die Strategie wertet Kerzen-Hochs und -Tiefs aus, um Treffer zu erkennen.
- **Trailing-Stop**: Aktiviert wenn `TrailingStopPips > 0`. Sobald der offene Gewinn `TrailingStopPips + TrailingStepPips` überschreitet, wird der Stop um `TrailingStopPips` hinter dem Preis her gezogen. Der Schritt-Parameter verhindert häufige Stop-Anpassungen in flachen Märkten.
- Alle Preisabstände werden aus Pips über den `PriceStep` des Instruments umgerechnet. Bei 3- oder 5-Dezimalstellen-Kursstellung entspricht ein Pip zehn Preisschritten, was das ursprüngliche Experten-Verhalten repliziert.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `OrderVolume` | Basisvolumen für jede Marktorder. |
| `CloseBySignal` | Wenn `true`, schließt die Strategie bestehende Positionen und öffnet die entgegengesetzte Richtung, wenn ein Ausbruchssignal erscheint. |
| `BreakPointPips` | Abstand von der täglichen Eröffnung erforderlich, um einen Ausbruch zu bestätigen. |
| `LastBarSizeMinPips` / `LastBarSizeMaxPips` | Minimale und maximale Körpergröße der Auslösekerze. |
| `TrailingStopPips` | Trailing-Stop-Abstand. Auf `0` setzen zum Deaktivieren. |
| `TrailingStepPips` | Zusätzliche Bewegung vor jeder Trailing-Anpassung erforderlich. |
| `StopLossPips` | Optionaler fester Stop-Loss. `0` deaktiviert ihn. |
| `TakeProfitPips` | Optionaler fester Take-Profit. `0` deaktiviert ihn. |
| `CandleType` | Intraday-Kerzenserie für die Signalgenerierung. |

## Verwendungshinweise
- Die Strategie abonniert automatisch sowohl Intraday- als auch Tageskerzen. Stellen Sie sicher, dass der Datenanbieter die angeforderten Zeitrahmen unterstützt.
- Da die Logik fertige Kerzen auswertet, werden Orders beim Schlusskurs der Signalbar gesendet.
- Die Pip-Konvertierung setzt Forex-Kursstellung voraus. Überprüfen Sie die Standardwerte bei Anwendung der Strategie auf Instrumente mit unkonventionellen Tick-Größen.
