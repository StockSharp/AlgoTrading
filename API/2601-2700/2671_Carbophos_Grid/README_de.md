# Carbophos Grid Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Carbophos Grid Strategie ist eine direkte Konvertierung des MetaTrader 5 Expert Advisors "Carbophos". Sie pflegt kontinuierlich eine symmetrische Leiter von Kauf- und Verkauf-Limit-Orders rund um die aktuellen Bid/Ask-Preise. Die Strategie überwacht den aggregierten Floating-Profit des gesamten Grids und schließt alle Positionen, sobald entweder das gewünschte Gewinnziel oder der maximal tolerierte Drawdown erreicht wird. Nachdem die Position abgeflacht wurde und keine Pending Orders mehr vorhanden sind, wird die Leiter automatisch neu aufgebaut.

## Handelslogik
1. Wenn die Strategie startet und keine aktiven Orders oder offenen Positionen vorhanden sind, berechnet sie den Grid-Abstand in Kurseinheiten basierend auf dem konfigurierten Schritt in Pips und der Kurspräzision des Instruments. Fünf (konfigurierbar) Sell-Limit-Orders werden über dem besten Bid und die gleiche Anzahl von Buy-Limit-Orders unter dem besten Ask platziert.
2. Wenn eine Order gefüllt wird, wird die resultierende Position tick-by-tick mit Level1-Daten überwacht. Der Floating-PnL wird aus der Differenz zwischen dem aktuellen Exit-Preis (Bid für Long-Positionen, Ask für Short-Positionen) und dem volumengewichteten durchschnittlichen Eintrittspreis berechnet.
3. Sobald der Floating-Profit das konfigurierte Ziel überschreitet, oder der Floating-Verlust den Schutzschwellenwert verletzt, gibt die Strategie eine Marktorder aus, um die offene Position zu schließen, und storniert alle verbleibenden Limit-Orders. Die Statuskennung wird gelöscht, damit die Leiter bei der nächsten Preisaktualisierung neu aufgebaut wird.
4. Wenn alle Orders gefüllt werden, aber die Nettoposition auf null zurückkehrt (zum Beispiel weil der Markt durch das Grid umkehrt), löst das nächste Level1-Update eine neue Leiterplatzierung aus.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `ProfitTarget` | Floating-Profit (Geld), der das Schließen des gesamten Grids auslöst. |
| `MaxLoss` | Floating-Verlust (Geld), der einen Notausstieg erzwingt. |
| `StepPips` | Abstand zwischen aufeinanderfolgenden Grid-Levels in Pips. Wird intern in Kurseinheiten umgerechnet unter Verwendung der Tick-Größe und der Dezimalpräzision des Symbols. |
| `OrdersPerSide` | Anzahl der Limit-Orders, die über und unter dem aktuellen Marktpreis platziert werden. |
| `OrderVolume` | Volumen für jede Grid-Order. |

Alle Parameter unterstützen Optimierungsbereiche zur Vereinfachung der Experimente im StockSharp-Optimierer.

## Risikomanagement und Schutz
Die Strategie verwendet den eingebauten `StartProtection()`-Hook und wendet harte monetäre Stop/Profit-Levels auf Strategieebene an. Die Floating-PnL-Berechnung hängt von den `PriceStep`- und `StepPrice`-Einstellungen des Instruments ab. Wenn ein Schwellenwert erreicht wird, schließt die Strategie die Position mit einer Marktorder und storniert alle aktiven Limit-Orders, bevor die interne Grid-Kennung zurückgesetzt wird.

## Konvertierungshinweise
- Der ursprüngliche MQL5-Expert Advisor passte Pip-Werte für Drei- und Fünf-Dezimal-Forex-Symbole an. Der StockSharp-Port repliziert dieses Verhalten, indem der `PriceStep` des Exchanges mit 10 multipliziert wird, wenn das Instrument drei oder fünf Dezimalstellen hat.
- MetaTrader aggregiert Positionsgewinn, Provision und Swap pro Magic Number. In StockSharp wird der Floating-PnL aus dem gewichteten durchschnittlichen Eintrittspreis und dem aktuellen Bid/Ask-Preis neu berechnet, sodass explizite Provisionsbehandlung nicht erforderlich ist.
- Orderplatzierung, -stornierung und Positionsmanagement werden über die High-Level-`Strategy`-API implementiert (`BuyLimit`, `SellLimit`, `CancelActiveOrders`, `BuyMarket`, `SellMarket`).
- Das Grid wird ausschließlich aus Level1-Updates aktualisiert und repliziert das "OnTick"-Verhalten des Originalcodes ohne Einführung benutzerdefinierter Timer oder Sammlungen.

## Verwendung
1. Weisen Sie der Strategieinstanz die gewünschte `Security` und das gewünschte `Portfolio` zu, bevor Sie sie starten.
2. Passen Sie optional die Parameter an die Volatilität des Zielinstruments und die Risikobereitschaft an.
3. Starten Sie die Strategie. Sie abonniert sofort Level1-Daten, baut das erste Grid auf, sobald sowohl Bid- als auch Ask-Preise verfügbar sind, und verwaltet die Positionen automatisch weiter.
4. Überwachen Sie das Log auf Meldungen wie "Profit target reached" oder "Maximum loss reached", um zu wissen, wann das Grid zurückgesetzt wurde.

Stellen Sie sicher, dass das ausgewählte Instrument Level1-Updates mit den besten Bid- und Ask-Preisen liefert; andernfalls wird die Leiter nicht aufgebaut.
