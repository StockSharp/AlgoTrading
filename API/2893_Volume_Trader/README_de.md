# Volume Trader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
- Port des MetaTrader 5-Experten **"Volume trader" (ID 21050)** von Vladimir Karputov.
- Auf der hochrangigen StockSharp-Strategie-API neu erstellt.
- Handelt in Richtung der letzten Tick-Volumen-Änderung, während ein benutzerdefinierter Handelssitzungsfilter aktiv ist.

## Handelslogik
1. Abonniert Kerzen, die durch `CandleType` definiert sind (Standard: 1-Stunden-Zeitrahmen), und liest deren Tick-Volumen (`TotalVolume`).
2. Bei jeder abgeschlossenen Kerze vergleicht die Strategie die Volumina der **beiden vorherigen** geschlossenen Kerzen und ahmt das MQL5-Skript nach, das bei der Geburt einer neuen Kerze läuft.
3. Wenn das neuere Volumen höher als das davor ist und keine Long-Position vorhanden ist, kauft die Strategie `Volume` Kontrakte und deckt zusätzlich eine bestehende Short-Position ab.
4. Wenn das neuere Volumen niedriger als das davor ist und keine Short-Position vorhanden ist, verkauft die Strategie `Volume` Kontrakte und schließt zusätzlich eine bestehende Long-Position.
5. Handelssignale werden ignoriert, wenn die Eröffnungszeit des nächsten Balkens außerhalb des `[StartHour, EndHour]`-Fensters liegt. Der Standardbereich 09:00–18:00 repliziert die ursprünglichen Eingaben.
6. Standardmäßig sind kein Stop-Loss oder Take-Profit definiert; die Strategie kehrt bei gegensätzlichem Signal einfach um.

## Auftragsverwaltung
- Einstiegsaufträge werden über `BuyMarket` oder `SellMarket` gesendet, um die Position sofort beim Beginn einer neuen Kerze zu drehen.
- Wenn ein Umkehrsignal erscheint, handelt die Strategie automatisch die absolute Positionsgröße plus das konfigurierte `Volume` und stellt sicher, dass die vorherige Position geschlossen wird, bevor eine neue eröffnet wird.
- Es gibt keine eingebaute Positionsgrößenlogik außer dem festen `Volume`-Parameter.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `CandleType` | 1-Stunden-Zeitrahmen | Für die Tick-Volumen-Berechnung verwendete Kerzenserie. Anpassen, um mit dem im ursprünglichen Experten verwendeten Zeitrahmen übereinzustimmen. |
| `StartHour` | 9 | Inklusive Stunde (0–23), die den Beginn der Handelssitzung markiert. Signale vor dieser Stunde werden ignoriert. |
| `EndHour` | 18 | Inklusive Stunde (0–23), die das Ende der Handelssitzung markiert. Signale nach dieser Stunde werden ignoriert. |
| `Volume` | 0.1 | Ordervolumen für neue Einstiege. Wird auch beim Umkehren einer bestehenden Position verwendet. |

## Verwendungshinweise
- Sicherstellen, dass die Datenquelle Tick-Volumen in den Kerzen-Nachrichten bereitstellt. Wenn nur das tatsächlich gehandelte Volumen verfügbar ist, folgt das Verhalten diesen Daten.
- Den `CandleType`-Parameter mit dem Chartzeitrahmen abgleichen, den Sie von MetaTrader reproduzieren möchten.
- Erwägen, die Strategie mit externem Risikomanagement zu umhüllen (Stop-Loss, Take-Profit, tägliche Verlustlimits), falls dies die Handelsregeln erfordern.
- Die Strategie ruft `LogInfo` auf, wenn eine Position eröffnet wird, was die Überprüfung von Signalentscheidungen im Protokoll erleichtert.

## Unterschiede zur ursprünglichen MQL-Implementierung
- Verwendet die Kerzen-Abonnement-Pipeline von StockSharp anstatt `CopyTickVolume` manuell aufzurufen.
- Die Sitzungsfilterung basiert auf dem `CloseTime` der abgeschlossenen Kerze (der Startzeit des nächsten Balkens), um mit der MQL-Logik aligned zu bleiben, die bei Balkeneröffnung ausgeführt wird.
- Die Auftragsausführung wird über High-Level-API-Helfer (`BuyMarket`, `SellMarket`) abgewickelt statt über direkte `CTrade`-Aufrufe.
