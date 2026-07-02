# RRS Tangled EA Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **RRS Tangled EA-Strategie** ist eine StockSharp-Portierung des MetaTrader 4-Expertenberaters „RRS Tangled EA“. Das ursprüngliche System wählt die Handelsrichtung und das Symbol nach dem Zufallsprinzip aus, begrenzt gleichzeitig die Anzahl gleichzeitiger Aufträge und schützt den schwankenden Gewinn durch Trailing Stops und strenge Risikolimits. Die konvertierte Version konzentriert sich auf das aktuell ausgewählte Instrument und reproduziert das Zufallseintritts-, Nachlauf- und Risikomanagementverhalten mithilfe des High-Level-StockSharp API.

## Kernlogik
1. Abonnieren Sie die konfigurierte Kerzenserie und warten Sie auf fertige Kerzen.
2. Auf jeder Leiste:
   - Aktualisieren Sie die Trailing-Stop-Levels für bestehende Long- und Short-Körbe.
   - Überprüfen Sie die Stop-Loss- und Take-Profit-Abstände anhand der Kerzenhochs und -tiefs.
   - Bewerten Sie den schwebenden Gewinn aller offenen Einträge; alles schließen, wenn die Money-at-Risk-Schwelle überschritten wird.
   - Wenn der Handel erlaubt ist, der Spread akzeptabel ist und die Anzahl der Einträge unter dem Limit liegt, zeichnen Sie eine zufällige Ganzzahl in `[0, 3]`.
   - Öffnen Sie einen neuen Long, wenn der Zufallswert `1` ist, oder einen neuen Short, wenn der Wert `2` ist, und verwenden Sie dabei ein zufälliges Volumen zwischen den konfigurierten Grenzen.
3. Trailing-Stops folgen dem besten Geld-/Briefkurs, sobald sich der Preis um die Aktivierungsdistanz bewegt, und sichern Gewinne, wenn der Preis um die Trailing-Lücke zurückgeht.
4. Das Risikomanagement kann im Festgeldmodus oder prozentual zum aktuellen Kontostand erfolgen. Wenn der variable Verlust den konfigurierten Betrag überschreitet, werden alle Positionen sofort abgeflacht.

## Parameter
| Name | Beschreibung |
|------|-------------|
| `MinVolume` | Untergrenze für das zufällig generierte Handelsvolumen. |
| `MaxVolume` | Obergrenze für das zufällige Handelsvolumen. |
| `TakeProfitPips` | Zielentfernung in Pips, angewendet auf den durchschnittlichen Einstiegspreis des Warenkorbs. |
| `StopLossPips` | Schutzstoppdistanz in Pips, gemessen vom durchschnittlichen Einstiegspreis. |
| `TrailingStartPips` | Erforderliche Gewinndistanz, bevor die Trailing-Logik aktiviert wird. |
| `TrailingGapPips` | Es besteht eine Lücke zwischen dem Trailing Stop und dem besten Geld-/Briefkurs. |
| `MaxSpreadPips` | Maximal zulässiger Spread vor dem Öffnen eines neuen Zufallseintrags. |
| `MaxOpenTrades` | Maximale Anzahl gleichzeitiger Eingaben in beide Richtungen. |
| `RiskManagementMode` | Wechselt zwischen der Handhabung von Festgeld- und Saldo-Prozent-Risikobehandlungen. |
| `RiskAmount` | Höhe des Risikos (Währung oder Prozentsatz), das anhand der variablen Gewinn- und Verlustrechnung überwacht wird. |
| `TradeComment` | Optionaler Kommentar zur Buchhaltung, der aus Kompatibilitätsgründen mit der Quelle EA beibehalten wird. |
| `Notes` | Informationstext, der in der Strategiestatuszeichenfolge angezeigt wird. |
| `CandleType` | Kerzenserie zur Entscheidungsfindung. |

## Unterschiede zur MQL-Version
- Trades werden auf dem der Strategie zugewiesenen Instrument ausgeführt, anstatt zufällig Symbole aus der MetaTrader-Marktüberwachung auszuwählen. Dadurch bleibt die Implementierung mit den Einzelsicherheitsstrategien von StockSharp kompatibel.
- Die Auftragsverwaltung erfolgt für aggregierte Long/Short-Körbe und spiegelt wider, wie die ursprünglichen EA-Positionen mit denselben magischen Zahlen gruppiert wurden.
- Die Spread-Kontrolle basiert auf dem neuesten besten Geld-/Briefkurs aus dem Orderbuch und nicht auf den `MarketInfo`-Anrufen von MetaTrader.

## Nutzungshinweise
- Stellen Sie sicher, dass der angeschlossene Broker oder Simulator sowohl Geld- als auch Briefkurse bereitstellt, damit die Spread- und Trailing-Berechnungen korrekt bleiben.
- Stellen Sie `MinVolume` und `MaxVolume` innerhalb des zulässigen Lautstärkebereichs des Instruments ein. Die Strategie passt das zufällige Volumen automatisch an den Volumenschritt und die Volumengrenzen des Symbols an.
- Die Risikomanagementlogik schließt *alle* Geschäfte sofort, sobald der variable Verlust den konfigurierten Schwellenwert überschreitet; Bis zur nächsten Kerze werden keine neuen Positionen eröffnet.
