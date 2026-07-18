# TRAYLERv-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **TRAYLERv-Strategie** ist eine direkte Umsetzung des MetaTrader 4 Expertenberaters *TRAYLERv*. Der ursprüngliche Code fungierte eher als automatisierter Handelsmanager als als Signalgenerator; Es passte kontinuierlich Schutzaufträge für bestehende Positionen mithilfe von Bill Williams-Fraktalen an und ermöglichte es Händlern, ausstehende ausstehende Aufträge zu bereinigen. Dieser StockSharp-Port behält das gleiche Verhalten bei und nutzt gleichzeitig den High-Level-API für die Auftragsverwaltung und Kerzenabonnements.

Die Strategie eröffnet **keine** selbstständig Positionen. Es geht davon aus, dass Trades manuell oder durch eine andere Strategie erstellt werden, und übernimmt dann die Aufgabe, Stopps und Take-Profits gemäß der folgenden Logik aufrechtzuerhalten. Alle Kommentare und Konfigurationsnamen folgen dem alten EA, sodass erfahrene Benutzer das Verhalten schnell zuordnen können.

## Handelslogik
1. Abonnieren Sie die konfigurierte Kerzenserie (standardmäßig einminütige Kerzen) und zeichnen Sie jeden fertigen Balken auf. Fraktale Hochs und Tiefs werden erkannt, sobald fünf Kerzen verfügbar sind, was die Standard-MT4-Fraktaldefinition reproduziert.
2. Jedes Mal, wenn eine neue Kerze während einer geraden Minute schließt, überprüft die Strategie die aktuelle Nettoposition:
   - **Long-Positionen**: Suche nach dem neuesten Down-Fraktal innerhalb von `StopFractalDepth` Balken (Standard 7). Wenn ein Verkaufsstopp gefunden wird, platzieren oder verschieben Sie ihn unter das Fraktaltief abzüglich des aktuellen Spreads und eines Zwei-Punkte-Puffers. Wenn kein gültiges Fraktal vorhanden ist, verwenden Sie das Tief der Kerze drei Balken zurück minus zwei Punkte. Wenn eine Long-Position profitabel ist und Take-Profits aktiviert sind, suchen Sie nach dem neuesten Aufwärts-Fraktal innerhalb von `TakeProfitFractalDepth` Balken (Standard 21) und legen Sie ein Verkaufslimit leicht unter diesem Niveau fest, um der MetaTrader-Implementierung zu entsprechen.
   - **Short-Positionen**: Spiegeln Sie die Logik wider, indem Sie Up-Fraktale für den Trailing-Buy-Stop und Down-Fraktale für das Take-Profit-Ziel verwenden. Über den fraktalen Höchstständen werden Puffer hinzugefügt, um vorzeitige Stopps zu vermeiden.
3. Wenn `DeleteAllPendingOrders` aktiviert ist, storniert die Strategie jede aktive ausstehende Bestellung, die sie sehen kann. Alternativ entfernt `DeleteOwnPendingOrders` nur die ausstehenden Aufträge, die zum aktuellen Symbol gehören. Beide Optionen replizieren die manuellen Bereinigungsschalter des Originals EA.
4. Wenn keine Position offen ist, werden alle von der Strategie registrierten Schutzaufträge gelöscht, um das Auftragsbuch sauber zu halten.

## Risikomanagement
- Schutzaufträge werden mit Market-Order-Gegenstücken erstellt (`SellStop`, `BuyStop`, `SellLimit`, `BuyLimit`). Das Volumen der Schutzanordnung entspricht immer der absoluten Nettopositionsgröße.
- Trailing Stops und Take-Profits sind optional. Durch das Deaktivieren des Take-Profit-Parameters werden alle vorhandenen Limit-Orders entfernt, die Trailing-Logik bleibt jedoch erhalten.
- Spread-Informationen werden, sofern verfügbar, dem besten Geld-/Briefpaar entnommen. Wenn kein Spread gemessen werden kann, greift der Code auf die minimale Preiserhöhung des Instruments zurück, um zu vermeiden, dass Aufträge direkt zum aktuellen Preis platziert werden.
- Alle Preisniveaus werden auf die Tick-Größe des Instruments normalisiert, sodass die resultierenden Aufträge den Börsenanforderungen entsprechen.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `OrderVolume` | Empfohlene Standardlautstärke für manuelle Eingaben. Es wird aus Kompatibilitätsgründen mit dem ursprünglichen EA beibehalten und nicht intern verwendet. | `0.1` |
| `DeleteAllPendingOrders` | Bei `true` wird nach jeder Kerze jede aktive ausstehende Order auf der Verbindung abgebrochen. | `false` |
| `DeleteOwnPendingOrders` | Bei `true` werden nur die ausstehenden Aufträge für das aktuelle Symbol storniert. | `false` |
| `UseTakeProfit` | Ermöglicht eine fraktalbasierte Take-Profit-Berechnung. Bei Deaktivierung wird jede bestehende Take-Profit-Order entfernt. | `true` |
| `EnableSound` | Erhaltenes Legacy-Flag von MT4; der Vollständigkeit halber bereitgestellt, aber in StockSharp nicht verwendet. | `true` |
| `ShowCommentary` | Legacy-Schalter entspricht dem MT4-On-Chart-Kommentar. Es ist für Konfigurationsbildschirme verfügbar, hat aber keine Auswirkung auf den Port. | `true` |
| `StopFractalDepth` | Anzahl der Balken, die überprüft wurden, um ein Fraktal für den Trailing Stop zu finden. | `7` |
| `TakeProfitFractalDepth` | Anzahl der untersuchten Balken, um ein Fraktal für den Take-Profit zu finden. | `21` |
| `CandleType` | Datentyp, der für die primäre Kerzenserie verwendet wird. Standardmäßig ist ein Zeitrahmen von 1 Minute eingestellt. | `1 minute` Zeitrahmen |

## Implementierungshinweise
- Die Strategie nutzt den High-Level-Workflow `SubscribeCandles().Bind(...)` und verarbeitet nur fertige Kerzen, spiegelt die tickbasierte MT4-Schleife wider und vermeidet gleichzeitig vorzeitige Aktualisierungen.
- Die Fraktalerkennung wird manuell mithilfe einer fortlaufenden Liste von Kerzen-Snapshots implementiert. Dies reproduziert das Verhalten des MT4-Indikators `iFractals`, ohne sich auf zusätzliche StockSharp-Indikatoren zu verlassen.
- Die Bestellpreise werden auf den nächsten gültigen Tick gerundet und die Volumina berücksichtigen die Einschränkungen `VolumeStep`, `MinVolume` und `MaxVolume`, um die Börsenkompatibilität zu gewährleisten.
- Es ist keine Python-Übersetzung enthalten. Das Verzeichnis `PY` fehlt bewusst und entspricht den Anforderungen der Konvertierungsrichtlinien.
