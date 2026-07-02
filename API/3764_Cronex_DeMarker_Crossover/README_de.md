# Cronex DeMarker Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Cronex DeMarker Crossover-Strategie reproduziert den MetaTrader-Indikator **Cronex DeMarker** und wandelt ihn in ein automatisiertes Handelssystem um. Der ursprüngliche Indikator stellt den DeMarker-Oszillator zusammen mit zwei linear gewichteten gleitenden Durchschnitten (LWMAs) dar. Die Strategie spiegelt dieses Setup wider, bewertet bullische und bärische Übergänge zwischen den geglätteten Oszillatorlinien und wandelt sie in Marktaufträge um. Dadurch kann die Handelslogik sofort reagieren, wenn sich die Dynamik je nach Indikator vom Abwärts- zum Aufwärtsdruck (und umgekehrt) ändert.

## Indikatorkonstruktion
1. **DeMarker-Oszillator** – Misst die Beziehung zwischen der aktuellen Kerze und der vorherigen Kerze:
   - Wenn das aktuelle Hoch höher ist als das vorherige Hoch, entspricht der positive Druck der Differenz der Höchststände; andernfalls ist es Null.
   - Wenn das aktuelle Tief niedriger ist als das vorherige Tief, entspricht der Unterdruck dem Abstand zwischen den Tiefs; andernfalls ist es Null.
   - Die Summen von Über- und Unterdruck über `DeMarkerPeriod` bar bilden den Oszillatorwert `deMax / (deMax + deMin)`.
2. **Schneller LWMA** – Ein linear gewichteter gleitender Durchschnitt mit der Periode `FastMaPeriod` wird auf die DeMarker-Rohwerte angewendet, um die neuesten Oszillatoränderungen hervorzuheben.
3. **Langsamer LWMA** – Ein weiterer linear gewichteter gleitender Durchschnitt mit der Periode `SlowMaPeriod` glättet denselben DeMarker-Stream, um eine langsamere Bestätigungslinie zu bilden.

Die Strategie führt jede fertige Kerze diesem Indikatorstapel zu und stimmt dabei genau mit den Pufferberechnungen aus der ursprünglichen MQ4-Datei überein.

## Handelslogik
1. Warten Sie, bis der DeMarker-Oszillator und beide LWMAs vollständig geformt sind.
2. Berechnen Sie nach jeder abgeschlossenen Kerze den neuen DeMarker-Wert und aktualisieren Sie beide gleitenden Durchschnitte.
3. Erkennen Sie Übergänge zwischen der schnellen und langsamen LWMA-Serie:
   - **Bullish Crossover** – Der schnelle LWMA bewegt sich von unten nach oben über den langsamen LWMA. Die Strategie schließt jegliches Short-Engagement und eröffnet eine Long-Marktposition.
   - **Bearish Crossover** – Der schnelle LWMA bewegt sich von oberhalb nach unterhalb des langsamen LWMA. Die Strategie schließt alle Long-Positionen und eröffnet eine Short-Marktposition.
4. Aufträge werden übersprungen, solange die Strategie noch nicht festgelegt ist, während sie offline ist oder der Handel deaktiviert ist.

Bei entgegengesetzten Signalen werden die Positionen sofort umgekehrt. Das bestehende Exposure wird geschlossen, indem die erforderliche Menge zur neuen Marktorder hinzugefügt wird.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `DeMarkerPeriod` | Anzahl der Kerzen, die zum Aufbau des DeMarker-Oszillators verwendet werden. | `25` |
| `FastMaPeriod` | Periode des schnellen linear gewichteten gleitenden Durchschnitts, der auf neue Oszillatorwerte reagiert. | `14` |
| `SlowMaPeriod` | Zeitraum des langsamen linearen gewichteten gleitenden Durchschnitts, der die Richtung bestätigt. | `25` |
| `CandleType` | Von der Strategie verarbeitete Kerzenserie (Zeitrahmen oder anderer `DataType`). | `1 Hour` Zeitrahmen |

## Details zur Implementierung
- Verwendet die übergeordnete `SubscribeCandles` API. Indikatoren werden nur aktualisiert, wenn eine Kerze den Status `Finished` erreicht, um ein Neuzeichnen in der Mitte des Balkens zu vermeiden.
- Die Strategie basiert auf den integrierten Indikatoren `DeMarker` und `WeightedMovingAverage` von StockSharp, um die MQ4-Puffer originalgetreu nachzubilden.
- Es wird automatisch ein Diagrammbereich erstellt, in dem die Preiskerzen zusammen mit dem Oszillator und beiden gleitenden Durchschnitten zur visuellen Bestätigung dargestellt werden.
- `StartProtection()` wird beim Start aufgerufen, sodass der Positionsschutz genau einmal aktiviert wird, wie in den Projektrichtlinien gefordert.

## Nutzung
1. Hängen Sie die Strategie an das gewünschte Wertpapier an und weisen Sie den bevorzugten Kerzentyp zu (z. B. Kerzen mit 1-Stunden-Zeitrahmen).
2. Konfigurieren Sie den DeMarker und die gleitenden Durchschnittsperioden so, dass sie mit dem ursprünglichen Indikator übereinstimmen, oder optimieren Sie sie zur Optimierung.
3. Führen Sie die Strategie aus. Der Handel beginnt, sobald die Indikatoren vollständig gebildet sind und der Handel zulässig ist.
4. Beobachten Sie das gezeichnete Diagramm, um zu sehen, wie die DeMarker-Oszillator- und LWMA-Crossover-Signale die Einträge antreiben.
