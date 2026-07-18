# Manuelle Positionsverfolgungs-Panel-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Der ursprüngliche MQL5-Expertenberater verfügte über ein visuelles Bedienfeld, das es einem Händler ermöglichte, bis zu fünf Long- und fünf Short-Positionen manuell zu verwalten. Über Schaltflächen im Bedienfeld wurden bestehende Take-Profit-Werte gelöscht, neue Take-Profit-Preise aus dem Eintrag neu berechnet oder sie auf die Gewinnschwelle für die ausgewählten Tickets verschoben. Der StockSharp-Port automatisiert diese Schutzmaßnahmen ohne die visuelle Schnittstelle. Die Strategie überwacht die aggregierte Position für das konfigurierte Symbol und verwaltet dynamisch eine schützende Take-Profit-Reihenfolge, die den Panel-Workflow widerspiegelt.

Wichtige Automatisierungsschritte:

- Platzieren Sie einen Take-Profit zum Einstiegspreis plus/minus einer konfigurierbaren Pip-Distanz von MetaTrader, wenn eine Position erscheint.
- Erhöhen Sie optional den Take-Profit auf den durchschnittlichen Einstiegspreis, sobald sich der Markt um die gewünschte Anzahl von Pips in die günstige Richtung bewegt, und sichern Sie so effektiv einen Break-Even-Ausstieg.
- Berücksichtigen Sie die Freeze-/Stop-Distanzen von Brokern, wenn sie über Level1-Daten veröffentlicht werden, oder nähern Sie sie mithilfe des aktuellen Spreads und eines vom Benutzer gesteuerten Multiplikators an.
- Heben Sie die Schutzanordnung auf, wenn die Verwaltung deaktiviert oder die Position geschlossen wird, und halten Sie dabei das Verhalten im Einklang mit der Schaltfläche „TP löschen“ im Bedienfeld.

Die Klasse basiert ausschließlich auf hochrangigen StockSharp API-Methoden (`SubscribeLevel1`, `SellLimit`, `BuyLimit`, `ReRegisterOrder` usw.) und nutzt die automatische Volumen-/Preisnormalisierung, sodass sie an jedes vom Connector unterstützte Instrument angeschlossen werden kann.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| **Take-Profit-Distanz (Pips)** | Der Abstand von MetaTrader Pip wird zum Einstiegspreis hinzugefügt, wenn der schützende Take-Profit gebildet wird. |
| **Eintrittsbasierte Take-Profit aktivieren** | Ermöglicht die automatische Platzierung des aus dem Einstiegspreis abgeleiteten Take-Profits. Wenn die Strategie deaktiviert ist, reagiert sie nur auf Break-Even-Anforderungen. |
| **Break-Even ermöglichen** | Verschiebt den Take-Profit zurück auf den durchschnittlichen Einstiegspreis, sobald der Break-Even-Trigger erfüllt ist. |
| **Break-Even-Trigger (Pips)** | Es ist eine minimale günstige Bewegung (in MetaTrader Pips) erforderlich, bevor die Gewinnschwelle angewendet wird. Ein Wert von `0` wendet es sofort an. |
| **Long-Positionen verwalten** | Bei `true` wird die lange Seite der aggregierten Position verarbeitet. |
| **Short-Positionen verwalten** | Bei `true` wird die kurze Seite der aggregierten Position verarbeitet. |
| **Take-Profit entfernen, wenn deaktiviert** | Hebt die Schutzanordnung auf, wenn die Verwaltungsbedingungen nicht erfüllt sind (ähnlich der ursprünglichen Schaltfläche „TP löschen“). |
| **Protokollverwaltungsaktionen** | Aktiviert die Informationsprotokollierung für jede vom Algorithmus ausgeführte Aktion zum Erstellen, Ändern oder Abbrechen. |
| **Einfrierentfernungsmultiplikator** | Multiplikator, der verwendet wird, um die Freeze-/Stop-Abstände vom aktuellen Spread anzunähern, wenn die Börse keine expliziten Niveaus veröffentlicht. |

## Signale und Ausführungsregeln

1. Beim Start abonniert die Strategie Level1-Updates, um die besten Geld-/Briefkurse sowie optionale Freeze- und Stop-Levels zu verfolgen, die vom Gateway bereitgestellt werden.
2. Immer wenn ein neuer Trade erscheint, sich die Gesamtposition ändert oder neue Level-1-Daten eintreffen, bewertet die Strategie die Schutzlogik neu.
3. Wenn keine Position offen ist, wird jede bestehende Take-Profit-Order storniert.
4. Wenn eine Position aktiv ist und die entsprechende Seite aktiviert ist:
   - Das Basisziel ist der Einstiegspreis, verschoben um die konfigurierte Take-Profit-Distanz (sofern aktiviert).
   - Wenn die Gewinnschwelle aktiviert ist und sich der aktuelle Marktpreis weit genug bewegt hat, wird das Ziel auf den durchschnittlichen Einstiegspreis festgelegt.
   - Das Ziel wird angepasst, um die Freeze-/Stop-Distanzen zu berücksichtigen, indem es mit der aktuellen Marktnotierung verglichen wird.
   - Preis und Volumen werden über `PriceStep`/`VolumeStep` normalisiert, anschließend wird auf der Gegenseite eine Limit-Order registriert bzw. erneut registriert.
5. Wenn die Konfiguration die Verwaltung für die erkannte Seite deaktiviert, wird der vorhandene Take-Profit entfernt, wenn **Take-Profit entfernen, wenn deaktiviert** den Wert `true` hat.

## Hinweise zum Risikomanagement

- Der Algorithmus verwaltet nur Take-Profit-Aufträge. Stop-Loss-Level, Trailing-Logik oder Teilausstiege fallen nicht in den Geltungsbereich.
- Da das ursprüngliche Panel mit MetaTrader „Pips“ (Punkten) arbeitete, berechnet die Strategie die Pip-Größe automatisch aus `PriceStep` und der Instrumentengenauigkeit, um mit Forex-Symbolen kompatibel zu bleiben.
- Einfrier-/Stoppabstände der Stufe 1 werden eingehalten, sofern verfügbar. Wenn der Broker sie nicht sendet, ermöglicht der Multiplikatorparameter dem Benutzer, einen Sicherheitspuffer aus dem Live-Spread zu erstellen und so abgelehnte Änderungen zu verhindern.
- Die Strategie schafft keine neuen Markteintritte; Es ist für den Anschluss an diskretionäre oder externe Handelssysteme konzipiert, die bereits die Auftragsausführung verwalten.

## Nutzungstipps

1. Befestigen Sie die Strategie an dem Instrument, das Sie überwachen möchten, und stellen Sie sicher, dass der Anschluss Informationen der Stufe 1 bereitstellt.
2. Konfigurieren Sie den Pip-Abstand so, dass er mit dem Schutzziel übereinstimmt, das Sie zuvor in MetaTrader verwendet haben.
3. Aktivieren Sie das Break-Even-Modul, wenn Sie möchten, dass der Schutz Gewinne sperrt, sobald eine Position günstig wird. Lassen Sie den Auslöser auf Null, um einen sofortigen Break-even zu erreichen.
4. Deaktivieren Sie die Verwaltung für eine Seite (Long oder Short), wenn Sie die diskretionäre Kontrolle über diese Richtung behalten möchten.
5. Überwachen Sie die Protokollausgabe, wenn **Protokollverwaltungsaktionen** aktiv sind, um zu überprüfen, ob die Bestellungen wie erwartet erstellt oder angepasst werden.
