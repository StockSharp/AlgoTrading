# TurnGrid-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **TurnGrid-Strategie** repliziert das Verhalten des ursprünglichen MQL5 Expert Advisors `TurnGrid.mq5`. Es erstellt ein symmetrisches Preisgitter um den aktuellen Marktpreis und wechselt zwischen Long- und Short-Orders, wenn der Preis von einer Gitterzelle zur anderen wandert. Die Strategie gleicht offene Aufträge kontinuierlich neu aus, um sowohl ein bullisches als auch ein bärisches Engagement aufrechtzuerhalten, bis das konfigurierte Aktienziel erreicht ist.

Bei der Konvertierung wird das übergeordnete API von StockSharp verwendet: Kerzenabonnements steuern die Rasteraktualisierungen, Marktaufträge verwalten Ein- und Ausstiege und das Risikomanagement wird durch Strategieparameter ausgedrückt. Alle Kommentare wurden ins Englische übersetzt und die Benennung folgt den StockSharp-Konventionen.

## Handelslogik

1. Wenn die Strategie startet, erfasst sie den letzten Kerzenschluss und erstellt ein Raster mit `4 * GridShares` Niveaus. Die mittlere Ebene wird auf den aktuellen Preis eingestellt, die oberen Ebenen skalieren um `1 + GridDistance` und die unteren Ebenen skalieren um `1 - GridDistance`.
2. In der Mitte des Rasters wird eine erste Marktkauforder platziert. Sein Volumen wird aus dem verfügbaren Budgetanteil (`Balance / GridShares`) und einer inkrementellen Einsatzformel berechnet, die von der MQL-Version übernommen wurde.
3. Jede fertige Kerze aktualisiert den aktuellen Rasterindex basierend auf dem Schlusskurs. Wenn sich der Index ändert:
   - Positionen, die mit Tickets verknüpft sind, die zwei Ebenen vom neuen Index entfernt sind, werden geschlossen (Kauftickets unter dem Preis werden verkauft, Verkaufstickets über dem Preis werden zurückgekauft).
   - Es werden neue Positionen eröffnet, um sowohl Long- als auch Short-Anker auf dem aktiven Niveau zu halten. Wenn keine Seite vorhanden ist, öffnet die Strategie die Seite mit weniger aktiven Positionen, um das Risiko auszugleichen.
4. Die Gebühren werden über den Parameter `FeeRate` angenähert. Jede ausgeführte Bestellung trägt zu einer laufenden Gebühr bei, die bei der Bewertung der Leistung verwendet wird.
5. Wenn das Kontoguthaben (nach Abzug der kumulierten Gebührenschätzung) den Anfangssaldo um `EquityTakeProfit` übersteigt, schließt die Strategie die Nettoposition und baut das Raster um den neuesten Preis herum neu auf.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `GridDistance` | Relativer Abstand zwischen benachbarten Rasterebenen. | `0.01` |
| `GridShares` | Maximale Anzahl gleichzeitiger Rasterpositionen, die aktiv sein können. | `50` |
| `EquityTakeProfit` | Prozentualer Anstieg gegenüber dem Anfangssaldo, der zum Zurücksetzen des Rasters erforderlich ist. | `0.02` |
| `FeeRate` | Geschätzte Transaktionsgebühr pro Trade, angewendet auf das ausgeführte Volumen. | `0.0008` |
| `CandleType` | Kerzenserien dienten als Antrieb für die Strategie. | `1` Minuten Zeitrahmen |

## Implementierungshinweise

- Das Kerzenabonnement wird über `SubscribeCandles(CandleType)` abgewickelt und die Strategie reagiert nur auf fertige Kerzen und entspricht der tickgesteuerten Logik des ursprünglichen EA, während die Kompatibilität mit StockSharp gewahrt bleibt.
- Der Rasterstatus wird in einem kompakten Array von `GridLevel`-Strukturen gespeichert, die Preisanker, boolesche Flags und Ticketvolumina für verzögerte Schließungen enthalten.
- Die Auftragsgrößen folgen der ursprünglichen Formel für die inkrementelle Kapitalzuteilung, mit zusätzlicher Normalisierung durch die Wertpapiereinstellungen `VolumeStep`, `VolumeMin` und `VolumeMax`.
- Eigenkapitalbasierte Resets warten darauf, dass die aktuelle Nettoposition geschlossen wird, bevor das Raster neu aufgebaut wird, um saubere Übergänge zwischen Handelszyklen sicherzustellen.

## Dateien

- `CS/TurnGridStrategy.cs` – C#-Implementierung der Strategie unter Verwendung von StockSharp High-Level-Konstrukten.
- `README.md` – Englische Dokumentation (diese Datei).
- `README_zh.md` – Vereinfachte chinesische Dokumentation.
- `README_ru.md` – Russische Dokumentation.
