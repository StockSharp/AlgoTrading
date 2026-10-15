# DeMarker gewinnt Positionsvolumenstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine StockSharp-Portierung des MetaTrader-Expertenberaters *"DeMarker gewinnt Positionsvolumen"*. Es verwendet den DeMarker-Oszillator, um überverkaufte und überkaufte Extremwerte zu erkennen und das Risiko schrittweise zu erhöhen, wenn der Markt in einem angespannten Zustand bleibt. Die Implementierung arbeitet mit abgeschlossenen Kerzen und stellt sicher, dass nur ein Signal pro Balken verarbeitet wird.

Die C#-Version konzentriert sich auf die zentrale diskretionäre Logik des ursprünglichen Skripts und übernimmt gleichzeitig die übergeordnete StockSharp API. Ordermanagement, Volumenwachstum und optionales Umkehrverhalten sind über Strategieparameter verfügbar, sodass der Algorithmus an unterschiedliche Märkte und Zeitrahmen angepasst werden kann.

## Parameter
- **DeMarker-Zeitraum** – Anzahl der vom DeMarker-Indikator verwendeten Kerzen.
- **Upper Level** – Oszillatorschwelle, die eine kurze Belichtung vorbereitet (Standard `0.7`).
- **Unterer Pegel** – Oszillatorschwelle, die Langzeitbelichtung vorbereitet (Standard `0.3`).
- **Handelsvolumen** – Marktauftragsvolumen, das bei jedem Signal übermittelt wird.
- **Nur eine Position** – wenn diese Option aktiviert ist, wird die Strategie vor der Eröffnung eines neuen Handels abgeflacht, sodass das Nettoengagement niemals Long- und Short-Positionen vermischt.
- **Umgekehrte Signale** – tauscht Kauf- und Verkaufsauslöser aus und verwandelt die Strategie in eine konträre oder trendfolgende Version.
- **Kerzentyp** – Zeitrahmen der Kerzen, die für die Indikator- und Signalbewertung verwendet werden.

## Handelslogik
1. Für den ausgewählten Zeitraum wird ein Kerzenabonnement eröffnet und in einen DeMarker-Indikator eingespeist.
2. Wenn die letzte fertige Kerze schließt, wird der aktuelle DeMarker-Wert mit den konfigurierten Werten verglichen.
3. Ohne Umkehrung:
   - Wenn DeMarker unter dem unteren Niveau liegt, versucht die Strategie, eine Long-Position aufzubauen oder auszubauen.
   - Wenn DeMarker über dem oberen Niveau liegt, versucht die Strategie, eine Short-Position aufzubauen oder auszubauen.
4. Wenn die Umkehrung aktiviert ist, wird die Bedeutung der Niveaus umgekehrt (extreme Tiefs lösen Short-Positionen aus und extreme Hochs lösen Long-Positionen aus).
5. Der Algorithmus merkt sich die Balkenzeit des zuletzt ausgeführten Handels, um mehrere Einträge bei derselben Kerze zu vermeiden.

## Positionsmanagement
- Vor dem Richtungsumkehr prüft die Strategie den nicht realisierten Gewinn der bestehenden Position. Das entgegengesetzte Engagement wird nur geschlossen, wenn der aktuelle Kerzenpreis den Handel mit einem positiven Ergebnis verlässt, was das Schutzverhalten des ursprünglichen EA widerspiegelt.
- Positionsdurchschnitte werden intern verfolgt. Wenn weitere Aufträge in die gleiche Richtung hinzugefügt werden, wird der Durchschnittspreis neu berechnet, um die Rentabilität korrekt zu bewerten.
- Der optionale Parameter *Only One Position* erzwingt einen Flat-Status vor der Eingabe eines neuen Handels, was hilfreich ist, wenn Sie im Nettopositionsmodus arbeiten.
- `StartProtection()` wird aufgerufen, sobald die Strategie beginnt, um sicherzustellen, dass eine Notfallliquidation verfügbar bleibt, wenn die Position ungleich Null wird und der Algorithmus stoppt.

## Notizen
- Die Konvertierung ist für die übergeordnete Ebene StockSharp API konzipiert und basiert nicht auf benutzerdefinierten Sammlungen oder direkten Abfragen von Indikatorwerten.
- Risikogrößenmodelle aus der Version MetaTrader (feste Marge, prozentuales Risiko usw.) werden bewusst auf den konstanten Parameter `Trade Volume` vereinfacht. Passen Sie die Positionsgröße extern an, wenn eine dynamische Risikokontrolle erforderlich ist.
- Da Ausführungen mit Marktaufträgen zu Kerzenschlusskursen simuliert werden, denken Sie daran, die Konfiguration anhand der tatsächlichen Broker-Ausführungs- und Slippage-Anforderungen zu validieren.
