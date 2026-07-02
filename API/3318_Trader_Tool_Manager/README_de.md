# TraderToolEA-Handpanel (StockSharp-Port)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Zusammenfassung

Der ursprüngliche MetaTrader-4-Expert-Advisor **TraderToolEA v1.8** ist kein autonomer Handelsroboter, sondern ein Bedienpanel, das diskretionären Tradern beim Verwalten von Orders, Grids und Schutzniveaus hilft. Dieser Port bildet das Panel im StockSharp-Framework nach. Statt Chart-Buttons stellt die Strategie boolesche Parameter bereit, die wie Umschalter funktionieren: Setzen Sie sie in der GUI oder in Skripten auf `true`, um die jeweilige Aktion auszulösen.

Übersetzte Kernfunktionen:

* Marktorder-Abkürzungen zum Öffnen oder Schließen von Long-/Short-Exposure.
* Automatische Platzierung symmetrischer Grids aus Stop- oder Limit-Pending-Orders.
* Selektive Stornierung von Pending Orders (Buy/Sell/All) mit optionaler Orphan-Bereinigung.
* Virtuelle Stop-Loss-, Take-Profit-, Trailing-Stop- und Break-even-Verwaltung über Level1-Quotes.
* Auto-Sizing-Option, die die MetaTrader-Lotberechnung (`AccountBalance / LotSize * RiskFactor`) imitiert.

Die gesamte Logik verwendet ausschließlich die High-Level-API: Level1-Abonnements, Order-Hilfsmethoden (`BuyStop`, `SellLimit`, `CancelOrder`...) und die integrierten Logging-Funktionen.

## Parameter

| Name | Beschreibung |
| --- | --- |
| `Use Auto Volume` | Wenn `true`, berechnet die Strategie die Lotgröße aus Portfoliowert und `Risk Factor`; andernfalls wird das feste `Order Volume` verwendet. |
| `Risk Factor` | Multiplikator auf den Portfoliowert bei der Auto-Volumen-Berechnung. Entspricht der MT4-Eingabe `RiskFactor`. |
| `Order Volume` | Manuelle Lotgröße für jede Markt- oder Pending-Order, wenn Auto-Sizing deaktiviert ist. |
| `Distance (pips)` | Abstand (in MetaTrader-Pips) zwischen gestaffelten Pending Orders. Gilt für Stop- und Limit-Grids. |
| `Layers` | Anzahl zusätzlicher Pending Orders pro Befehl. `1` entspricht einem einzelnen EA-Buttondruck, höhere Werte simulieren mehrere Betätigungen. |
| `Delete Orphans` | Wenn aktiviert, storniert die Strategie automatisch nicht zugeordnete Pending Orders, damit Buy/Sell-Grids nach Teilausführungen ausgeglichen bleiben. |
| `Enable Stop Loss` / `Stop Loss (pips)` | Aktiviert feste Stop-Loss-Überwachung in Pips relativ zum durchschnittlichen Einstiegspreis. |
| `Enable Take Profit` / `Take Profit (pips)` | Aktiviert feste Take-Profit-Überwachung in Pips. |
| `Enable Trailing` / `Trailing (pips)` | Aktiviert virtuelle Trailing-Stop-Verwaltung. Der Trail wird erst scharf, wenn sich der Preis mindestens `Trailing` Pips zugunsten der Position bewegt. |
| `Enable Break-Even` / `Break-Even Trigger` / `Break-Even Lock` | Sobald der Preis um die Triggerdistanz voranschreitet, wird der Stop auf Einstiegspreis plus Lock-Offset (Longs) oder minus Offset (Shorts) verschoben. |
| Befehls-Umschalter (`Open Buy`, `Place Buy Stops`, `Delete Sell Limits`, ...) | Boolesche Parameter, die EA-Buttons nachahmen. Setzen auf `true` führt die Aktion aus; die Strategie setzt sie danach auf `false` zurück. |

## Order-Ablauf

1. **Datenfeed:** Die Strategie abonniert nur `DataType.Level1`. Best-Bid/Ask-Updates treiben Schutzlogik und Grid-Platzierungen.
2. **Volumennormalisierung:** Vor jeder Order wird das gewünschte Volumen auf `VolumeStep` des Instruments gerundet und zwischen `MinVolume` und `MaxVolume` begrenzt. Fehlen Metadaten, wird der Rohwert verwendet.
3. **Pending Orders:** Stop- und Limit-Grids werden um das jüngste Bid/Ask gebaut. Preise werden am Preisschritt des Instruments ausgerichtet, um Ablehnungen durch die Matching Engine zu vermeiden.
4. **Orphan-Kontrolle:** Wenn `Delete Orphans` aktiviert ist, hält die Strategie die Anzahl der Buy- und Sell-Pending-Orders symmetrisch, indem sie nach Ausführungen oder manuellen Stornierungen die überschüssige Seite storniert. Dieselbe Logik gilt unabhängig für Stop- und Limit-Grids.
5. **Virtueller Schutz:** Stop-Loss, Take-Profit, Trailing Stop und Break-even sind als *virtuelle* Guards implementiert. Wird eine Schwelle verletzt, sendet die Strategie eine schließende Marktorder für das Restvolumen und setzt den internen Trailing-/Break-even-Zustand zurück.

## Unterschiede zur MetaTrader-Implementierung

* Grafische Komponenten (Buttons, Textfelder, Farben, Sounds) werden durch StockSharp-Parameter und Logs ersetzt. Jede Aktion schreibt einen informativen Eintrag über `AddWarningLog` oder den Standardlogger.
* Schutzlogik arbeitet auf Level1-Updates und schließt Positionen direkt, statt Stoppreise einzelner Orders zu ändern. Dadurch bleibt das Verhalten bei Brokern konsistent, die keine MetaTrader-artigen Stop-Orders unterstützen.
* Die MT4-`ManageOrders`-Modi (ID/manual/all/own) werden auf den Strategiebereich reduziert: Nur von dieser Strategie erstellte Orders werden verfolgt und verwaltet.
* Automatische Lotgröße nutzt die Portfoliobewertung statt `AccountBalance()`, behält aber Formel und Rundungsregeln bei.

## Nutzungstipps

1. Konfigurieren Sie Instrument-Metadaten (`PriceStep`, `VolumeStep`, `MinVolume`, `LotSize`, ...) in Ihrer Verbindung, damit Pip-Konvertierung und Volumenrundung zu den Brokerregeln passen.
2. Binden Sie boolesche Befehlsparameter an Hotkeys oder UI-Buttons im StockSharp-Terminal, um die ursprüngliche Bedienung nachzubilden. Die Eigenschaften werden nach erfolgreicher Ausführung auf `false` zurückgesetzt.
3. Aktivieren Sie `Delete Orphans` bei symmetrischen Grids, damit übrig gebliebene Stops/Limits automatisch bereinigt werden, wenn eine Seite ausgelöst wird.
4. Überwachen Sie das Info-Log: Überspringt die Strategie eine Aktion (z. B. kein Bid/Ask oder berechnetes Volumen null), wird eine Warnung mit Grund erzeugt.
5. Da Schutz virtuell ist, muss die Strategie laufen, solange Positionen offen sind: Sie schließt Trades mit Marktorders, nicht über serverseitige Stop-Orders.

## Portierungshinweise

* Die Pipgröße spiegelt MetaTrader: Instrumente mit 3 oder 5 Dezimalstellen multiplizieren den Preisschritt mit 10, um Punkte in Pips umzuwandeln.
* Trailing Stops und Break-even folgen dem MQL-Codefluss: Sie werden erst bei Bewegung in den Gewinn scharf und verwenden Zustandsvariablen, die bei neuen Trades, Stornierungen oder Positionsumkehr zurückgesetzt werden.
* Der EA erlaubte mehrfaches Drücken von Buttons zum Erweitern von Grids. `Layers` emuliert dies, indem mehrere Pending Levels in einem Aufruf erstellt werden.
* Alle manuellen Steuerelemente behalten `SetCanOptimize(false)`, damit Optimierungsläufe keine Aktionen versehentlich auslösen.
