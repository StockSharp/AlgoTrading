# Buy Sell Stop Buttons-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Bildet den MetaTrader 4 Experten "Buy Sell Stop Buttons" in StockSharp nach.
- Stellt drei manuelle Parameter (`BuyRequest`, `SellRequest`, `CloseRequest`) bereit, die die Chart-Buttons emulieren.
- Implementiert dieselben Geldmanagement-Helfer: festes Geld-Take-Profit, prozentuales Take-Profit, Equity-Trailing-Lock, Break-Even und Pip-Trailing-Stops.
- Verwendet eine Ein-Minuten-Kerzensubskription ausschließlich als Heartbeat zur Auswertung der Verwaltungsregeln auf abgeschlossenen Balken.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `OrderLots` | Basis-Lotgröße bei einer manuellen Einstiegsanfrage. Spiegelt den `Lots` Extern-Eingang (`0.01` standardmäßig) wider. |
| `NumberOfTrades` | Anzahl der pro Anfrage gesendeten Tickets. Der C#-Port fasst das Volumen in einer einzigen Market-Order zusammen. |
| `UseTakeProfitInMoney` / `TakeProfitInMoney` | Aktivieren und Konfigurieren des direkten Geldziels, das alle Trades beim Erreichen schließt. |
| `UseTakeProfitPercent` / `TakeProfitPercent` | Aktivieren und Konfigurieren des Equity-Prozentziels. Die Strategie verwendet `Portfolio.CurrentValue` zur Annäherung an den Kontostand. |
| `EnableTrailing`, `TrailingProfitMoney`, `TrailingLossMoney` | Konfigurieren des Equity-Trailing-Blocks: Sobald der Gewinn `TrailingProfitMoney` überschreitet, wird der Höchstwert verfolgt und alle Trades schließen, wenn der Gewinn um `TrailingLossMoney` zurückgeht. |
| `UseBreakEven`, `BreakEvenTriggerPips`, `BreakEvenOffsetPips` | Stop nach Break-Even plus Offset verschieben, nachdem die Position die konfigurierte Pip-Distanz verdient hat. |
| `StopLossPips`, `TakeProfitPips`, `TrailingStopPips` | Ticket-Verwaltungseinstellungen in Pip-Distanzen in StockSharp umgewandelt. |
| `CandleType` | Kerzenserie, die den Heartbeat antreibt (standardmäßig Ein-Minuten-Kerzen). |
| `BuyRequest`, `SellRequest`, `CloseRequest` | Manuelle Befehle, die die originalen Chart-Buttons ersetzen. Die Flags werden nach erfolgreicher Aktion automatisch zurückgesetzt. |

## Handelslogik
1. `OnStarted` abonniert die konfigurierte Kerzenserie, setzt das Basis-`Volume` und aktiviert den integrierten Positionsschutz.
2. Jede abgeschlossene Kerze löst den folgenden Workflow aus:
   - Manuelle Befehle werden ausgewertet: Kauf und Verkauf senden eine Market-Order mit `OrderLots * NumberOfTrades` Volumen, optional eine entgegengesetzte Position ausgleichend; Schließanfragen glätten die Strategie.
   - Geldziele werden der Reihe nach geprüft: fester Betrag, Eigenkapitalprozentsatz, dann der Equity-Trailing-Lock.
   - Break-Even- und Pip-Trailing-Stops passen interne Stop-Levels basierend auf dem durchschnittlichen Einstandspreis an.
   - Statische Stop-Loss/Take-Profit-Abstände werden durchgesetzt.
   - Optionaler Bollinger-Band-Ausstieg schließt Longs, die das obere Band berühren, oder Shorts, die das untere Band berühren (20 Perioden, Breite 2).
3. Der offene Gewinn wird mit `Security.PriceStep`/`Security.StepPrice` berechnet, wenn verfügbar; andernfalls wird ein Preisdifferenz-Fallback verwendet.

## Unterschiede zur MQL-Version
- MetaTrader erlaubte abgesicherte Positionen; StockSharp verrechnet die Exposition, daher neutralisieren Kauf-/Verkaufsanfragen zunächst entgegengesetzte Positionen.
- Monatliche MACD-basierte Ausstiege (`Close_BUY`/`Close_SELL`) fehlen, da sie im Originalskript nie aufgerufen wurden.
- Automatische Volumenskalierung via `MaximumRisk`/`DecreaseFactor` wird durch den expliziten `OrderLots`-Parameter ersetzt. Der MQL-Helfer war auf Kontohistorie angewiesen, die in diesem Port nicht verfügbar ist.
- Stop-Anpassungen werden durch abgeschlossene Kerzen statt rohe Ticks gesteuert, gemäß den Repository-Richtlinien.
- Indikatorwerte werden durch `Bind` verarbeitet, direkte Sammlungen oder manuelle Verlaufspuffer werden vermieden.

## Verwendungshinweise
- `BuyRequest`, `SellRequest` und `CloseRequest` unter der Gruppe "Manuelle Steuerungen" bei Optimierungsläufen deaktiviert lassen.
- Equity-Trailing-Lock und Geld-Take-Profit-Logik benötigen `Security.StepPrice` zur Umrechnung des Gewinns in Währung. Wenn nicht verfügbar, verwendet der Fallback reine Preisdifferenzen.
- Break-Even und Trailing-Stops verwenden die Pip-Größe des Instruments, abgeleitet aus `MinPriceStep`/`PriceStep` und Dezimalstellen.
- Es gibt keine Python-Übersetzung, wie gewünscht.

## Tests
- Keine automatisierten Tests wurden geändert; die Strategie integriert sich in die bestehende Lösungsstruktur und verlässt sich auf manuelle Parameter-Umschaltungen zur Überprüfung.
