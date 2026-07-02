# Eine Stop-Loss-/Take-Profit-Strategie für einen Preis
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Utility-Strategie repliziert das MetaTrader-Skript „One Price SL TP“ in StockSharp. Anstatt Geschäfte zu eröffnen, überwacht der Algorithmus die aktuelle Position des konfigurierten Instruments und stellt sicher, dass beide Schutzaufträge auf einen einzigen vom Benutzer festgelegten Zielpreis ausgerichtet sind.

Immer wenn der Parameter **`ZenPrice`** über Null liegt, vergleicht die Strategie ihn mit den Live-Bid/Ask-Kursen:

- Für eine **Long-Position**: Wenn `ZenPrice` höher als der Briefkurs ist, wird eine Take-Profit-Limit-Order zu diesem Preis platziert; Wenn `ZenPrice` niedriger als das Gebot ist, wird stattdessen eine Stop-Loss-Stop-Order registriert.
- Für eine **Short-Position**: Wenn `ZenPrice` niedriger als das Gebot ist, wird es zur Take-Profit-Limit-Order; Wenn `ZenPrice` höher als der Brief ist, wird es zur Stop-Loss-Stop-Order.

Wenn der Preis zwischen Geld- und Briefkurs fällt, wird nichts gesendet, sodass die vorherige Schutzorder unberührt bleibt. Sobald die Position geschlossen oder der Parameter auf Null zurückgesetzt wird, werden alle Schutzaufträge automatisch aufgehoben.

## Wie es funktioniert

1. Abonniert Level1-Daten, um aktuelle Geld-/Briefkurse zu erhalten, die für die Richtungsprüfungen erforderlich sind.
2. Verfolgt das Volumen und die Richtung der aktuellen Strategieposition. Es wird davon ausgegangen, dass Positionen manuell oder durch andere Strategien erstellt werden.
3. Berechnet bei jeder Kurs-, Positions- oder persönlichen Handelsaktualisierung neu, zu welcher Seite des Marktes der `ZenPrice` gehört, und erstellt den entsprechenden Schutzauftragstyp.
4. Normalisiert den angeforderten Preis mithilfe der Instrumentpreisstufe und rundet das Auftragsvolumen auf Börsenlimits, bevor etwas an den Handelskonnektor gesendet wird.
5. Verwendet `ReRegisterOrder`, um bereits aktive Schutzanordnungen zu ändern, anstatt sie zu stornieren, was dem Verhalten der direkten Änderung von MetaTrader entspricht.

## Parameter

- **`ZenPrice`** – absoluter Preis, der entweder als Stop-Loss- oder Take-Profit-Level verwendet werden sollte. Legen Sie den Wert auf `0` fest, um die Automatisierung zu deaktivieren. Standard: `0`.

## Praktische Hinweise

- Die Strategie übermittelt niemals Eintrittsaufträge. Es ist sicher, es neben diskretionären Handelsterminals oder anderen automatisierten Strategien zu starten.
- Schutzaufträge werden erst erteilt, nachdem der erste Level-1-Snapshot sowohl Geld- als auch Briefkurse liefert. Bis dahin wartet das Skript, genau wie die ursprüngliche MQL-Version auf den Terminal-Anführungszeichen beruhte.
- Wenn nur eine Seite des Marktes die Bedingung erfüllt (zum Beispiel liegt `ZenPrice` über dem Briefkurs, aber nicht unter dem Geldkurs), wird die andere Schutzorder storniert, um veraltete Preise zu vermeiden.
- Alle Kommentare im Code sind auf Englisch, während diese Dokumentation gemäß den Projektrichtlinien in mehreren Sprachen bereitgestellt wird.

## Unterschiede zum MetaTrader-Skript

- Das ursprüngliche Skript ändert die Stop-Loss- und Take-Profit-Felder eines vorhandenen Positionstickets. StockSharp stellt schützende Orders als explizite Stop- und Limit-Orders dar, daher erfolgt die Konvertierung stattdessen für an der Börse sichtbare Orders.
- MetaTrader passt den Preis automatisch an die Broker-Präzision an. In diesem Port wird das gleiche Verhalten über `NormalizePrice` reproduziert, das die Preisschritt- und Dezimaleinstellungen des Symbols nutzt.
- Das Positionsvolumen wird gerundet, um die Lot-Limits auszutauschen, bevor die Schutzaufträge gesendet werden. Dadurch wird die Kompatibilität mit Handelsplätzen sichergestellt, die bestimmte Lot-Schritte erfordern.
