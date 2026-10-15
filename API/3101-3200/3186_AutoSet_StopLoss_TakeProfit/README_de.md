# Auto Stop-Loss und Take-Profit-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Hilfsstrategie hängt automatisch schützende Stop-Loss- und Take-Profit-Orders an jede offene Position auf dem konfigurierten Instrument. Sie spiegelt das Verhalten des originalen MetaTrader-Experten "AutoSet SL TP" wider, indem sie die Liste der aktiven Positionen überwacht und die Broker-Abstandsbeschränkungen vor der Registrierung schützender Orders einhält.

Die Strategie eröffnet keine Trades von sich aus. Stattdessen überwacht sie das Volumen, die Richtung und den Ausführungspreis von Positionen, die manuell oder von anderen Strategien erstellt wurden. Sobald eine Long- oder Short-Position erscheint, berechnet der Algorithmus die gewünschten Stop-Loss- und Take-Profit-Niveaus in MetaTrader-Pips, passt die Niveaus an die Freeze- und Stop-Beschränkungen des Handelsplatzes an und sendet dann die entsprechenden marktschützenden Orders. Wenn die Position vollständig geschlossen ist, werden die schützenden Orders automatisch storniert.

## Funktionsweise

1. Abonniert Level1-Daten, um die besten Bid/Ask-Preise zusammen mit optionalen `StopLevel`- und `FreezeLevel`-Feldern des Brokers zu empfangen.
2. Wandelt die konfigurierten Pip-Abstände in absolute Preise um, indem die Symbol-Metadaten (Preisschritt und Dezimalgenauigkeit) verwendet werden. Fünf- und dreistellige Kurse werden automatisch um den Faktor zehn skaliert, um der MetaTrader-Pip-Semantik zu entsprechen.
3. Bei jeder Kursänderung oder persönlichen Trade-Benachrichtigung:
   - Ignoriert das Signal, wenn keine offene Position vorhanden ist oder wenn die Richtung nicht dem konfigurierten Filter entspricht (nur Kauf, nur Verkauf oder beide).
   - Berechnet den minimal zulässigen Abstand zwischen dem Marktpreis und einer Schutz-Order. Wenn der Broker keine Freeze/Stop-Niveaus veröffentlicht, fällt der Algorithmus auf drei Spreads multipliziert mit 1.1 zurück, um sicher außerhalb verbotener Zonen zu bleiben.
   - Bestimmt den Stop-Loss- und Take-Profit-Preis relativ zum aktuellen Ask (für Longs) oder Bid (für Shorts) und normalisiert das Ergebnis auf den Instrument-Preisschritt.
   - Platziert oder re-registriert Stop- oder Limit-Schutzorders mit dem genauen Positionsvolumen. Orders werden nur ersetzt, wenn sich der Zielpreis oder das Volumen ändert, was Exchange-Modifikationen minimiert.
4. Wenn das Positionsvolumen null wird, werden alle ausstehenden Schutzorders storniert. Die Strategie storniert auch bestehende Orders, wenn die Trade-Richtung durch den Filter nicht mehr erlaubt ist.

Da der Algorithmus ausschließlich auf externe Fills angewiesen ist, kann er mit diskretionärem Trading, Panels oder anderen automatisierten Systemen kombiniert werden, die Einträge verwalten, während diese Strategie eine konsistente Schutzhülle garantiert.

## Parameter

- **`StopLossPips`** – Abstand vom aktuellen Marktpreis zum Stop-Loss in MetaTrader-Pips. Ein Wert von `0` deaktiviert die Stop-Order. Standard: `50`.
- **`TakeProfitPips`** – Abstand vom aktuellen Marktpreis zum Take-Profit in MetaTrader-Pips. Ein Wert von `0` deaktiviert die Take-Profit-Order. Standard: `140`.
- **`DirectionFilter`** – gibt an, welche Positionsrichtung verwaltet wird:
  - `Buy` – nur Long-Exposure schützen.
  - `Sell` – nur Short-Exposure schützen.
  - `BuySell` – beide Seiten schützen (Standardverhalten im Originalskript).

## Praktische Hinweise

- Schutzorders werden immer mit dem absoluten Positionsvolumen erstellt. Wenn der Broker minimale oder maximale Lotgrößen vorschreibt, rundet die Strategie das Volumen auf den nächsten zulässigen Wert, bevor Orders platziert werden.
- Der Algorithmus verwendet `ReRegisterOrder`, um aktive Schutzorders anzupassen. Dies behält nach Möglichkeit dieselben Exchange-Order-Identifier und vermeidet unnötige Stornierungen.
- Der Fallback-Abstand (Spread × 3 × 1.1) verhindert, dass der Stop oder Take-Profit versteckte Exchange-Beschränkungen verletzt, wenn keine expliziten Freeze/Stop-Niveaus bereitgestellt werden.
- Da die Strategie keine Einträge verwaltet, kann sie vor oder nach dem Eröffnen von Positionen gestartet werden. Jede qualifizierende Position, die zum Zeitpunkt des Starts bereits existiert, wird unmittelbar nach der ersten Kursänderung geschützt.
- MetaTrader-„Pips" unterscheiden sich von Exchange-Preisschritten bei Symbolen mit drei oder fünf Dezimalstellen. Die Strategie spiegelt den originalen Expert Advisor, indem sie den Punktwert entsprechend multipliziert, um sicherzustellen, dass die konfigurierten Zahlen genau den MT5-Einstellungen entsprechen.

## Unterschiede zum MetaTrader-Experten

- Anstatt Stop- und Take-Profit-Attribute in der Position zu modifizieren, verwaltet StockSharp explizite schützende Stop- und Limit-Orders. Dieser Ansatz hält die Logik vollständig transparent im StockSharp-Orderbuch.
- Die StockSharp-Version verwendet Level1-Marktdaten, um Broker-Beschränkungsniveaus zu rekonstruieren. Wenn der Anbieter unterschiedliche Feldnamen für Freeze- oder Stop-Abstände bereitstellt, entdeckt die Strategie diese automatisch durch Reflexion auf dem `Level1Fields`-Enum.
- Jeder Code-Kommentar und jede Log-Nachricht ist auf Englisch, um mit den Kodierungsrichtlinien konsistent zu bleiben, während die Dokumentation für Endbenutzer auf Russisch und Chinesisch lokalisiert ist.
