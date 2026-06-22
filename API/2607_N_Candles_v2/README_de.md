# N Candles v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Strategie sucht nach einer konfigurierbaren Anzahl aufeinanderfolgender Kerzen, die in derselben Richtung schließen. Sobald die Streak-Länge erreicht ist, öffnet sie eine Marktposition in Richtung des erkannten Momentums. Die Implementierung spiegelt den ursprünglichen MetaTrader 5 Expert Advisor "N- candles v2" wider und konzentriert sich auf geschlossene Kerzen, um vorzeitige Signale zu vermeiden.

## Strategielogik
1. Die ausgewählte Kerzenserie abonnieren und auf vollständig geschlossene Kerzen warten.
2. Jede Kerze als bullisch, bearisch oder neutral (Doji) kategorisieren. Doji-Kerzen setzen den Streak zurück.
3. Einen laufenden Zähler aufeinanderfolgender Kerzen mit identischer Richtung führen.
4. Wenn der Zähler den Schwellenwert `CandlesCount` erreicht, eine Marktorder in derselben Richtung senden. Die Ordergröße kombiniert das angeforderte `LotSize` mit etwaigem Gegenengagement, sodass die finale Nettoposition das beabsichtigte Vorzeichen und die Menge hat.
5. Den Eintrittspreis speichern und Schutzlevel mit den konfigurierten Stop-Loss- und Take-Profit-Abständen initialisieren.
6. Bei jeder neuen Kerze den Trailing Stop aktualisieren (wenn aktiviert) und Positionen schließen, wenn der Preis Stop-Loss, Trailing Stop oder Take-Profit berührt.

## Positionsmanagement
- Der anfängliche Stop-Loss und Take-Profit werden in Preisschritten (`Security.PriceStep`) gemessen. Ein Abstand von null deaktiviert das entsprechende Level.
- Trailing Stop ist optional. Wenn aktiviert, wird der Stop um `TrailingStopPips` nachgezogen, sobald der Preis sich günstig um mindestens `TrailingStepPips` über die letzte Stop-Position hinausbewegt.
- Das Schließen einer Position entfernt alle gecachten Level, sodass ein neuer Streak für den nächsten Einstieg erforderlich ist.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `CandlesCount` | Anzahl aufeinanderfolgender Kerzen, die in dieselbe Richtung schließen müssen, bevor gehandelt wird. | 3 |
| `LotSize` | Positionsgröße für jeden Einstieg. Gegenläufiges Engagement wird automatisch geschlossen. | 1 |
| `TakeProfitPips` | Take-Profit-Abstand in Preisschritten vom Eintrittspreis. | 50 |
| `StopLossPips` | Stop-Loss-Abstand in Preisschritten vom Eintrittspreis. | 50 |
| `TrailingStopPips` | Trailing-Stop-Abstand in Preisschritten. Auf 0 setzen, um Trailing zu deaktivieren. | 10 |
| `TrailingStepPips` | Zusätzlicher Weg, den der Preis zurücklegen muss, bevor der Trailing Stop nachgezogen wird. | 4 |
| `CandleType` | Kerzen-Zeitrahmen für Signalberechnungen. | 1-Stunden-Kerzen |

## Hinweise
- Die Strategie funktioniert mit jedem Instrument, das einen gültigen `PriceStep` liefert. Wenn das Instrument null meldet, wird `1` als Fallback verwendet, entsprechend dem Verhalten des Quellskripts.
- Signale werden nur auf abgeschlossenen Kerzen generiert, was ein konsistentes Verhalten zwischen Backtesting- und Live-Trading-Umgebungen gewährleistet.
