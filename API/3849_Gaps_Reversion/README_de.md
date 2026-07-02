# Gap-Reversion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **Gap-Reversion-Strategie** ist eine direkte Portierung des MetaTrader 4 Expertenberaters `gaps.mq4`. Das System überwacht 15-Minuten-Kerzen und Toilette
ks für das Öffnen von Lücken, die außerhalb des Hoch-/Tief-Bereichs der vorherigen Kerze auftreten. Wenn eine solche Lücke auftritt, wird die Strategie sofort, z
Bringt den Markt in die Richtung der erwarteten Mean-Reversion-Bewegung.

Die StockSharp-Version folgt der ursprünglichen Logik und stützt sich dabei auf das High-Level-Kerzenabonnement API. Das gesamte Handelsmanagement ist
Dies erfolgt mit Marktaufträgen und es werden keine festen Schutzaufträge erteilt, was das Verhalten im MQL-Code widerspiegelt.

## Handelsregeln

1. Abonnieren Sie 15-Minuten-Kerzen (konfigurierbar über den Parameter `CandleType`).
2. Behalten Sie das Hoch und Tief der zuvor abgeschlossenen Kerze bei.
3. Wenn eine neue Kerze beginnt:
   - Berechnen Sie den Lückenpuffer: `(MinGapSize + spreadInSteps) * pointValue`.
   - Wenn der Eröffnungspreis **über** `previousHigh + gapBuffer` liegt, eröffnen Sie eine **Short**-Position.
   - Wenn der Eröffnungspreis **unter** `previousLow - gapBuffer` liegt, eröffnen Sie eine **Long-Position**.
4. Es ist nur ein Trade pro Kerze erlaubt. Sobald eine Order aufgegeben wurde, wartet die Strategie auf die nächste Kerze, bevor sie eine neue Kerze generiert
ignal.

Die Spread-Komponente verwendet den aktuell besten Geld-/Briefkurs, sofern verfügbar. Wenn keine Kursdaten bereitgestellt werden, fällt die Strategie auf eine Sünde zurück
gle-Preisschritt als konservativer Puffer.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `MinGapSize` | `1` | Mindestlückengröße in Preisschritten, die vor dem Absenden einer Bestellung überschritten werden muss. |
| `GapVolume` | `0.1` | Auftragsvolumen für durch Lücken ausgelöste Markteintritte. |
| `CandleType` | `15m TimeFrame` | Für Berechnungen verwendeter Kerzentyp (standardmäßig 15-Minuten-Kerzen). |

Alle Parameter sind als `StrategyParam<T>` registriert und unterstützen die Optimierung im StockSharp Designer oder anderen Tools.

## Implementierungshinweise

- Verwendet `SubscribeCandles` mit `Bind`, um nur fertige Kerzen zu verarbeiten.
- Merkt sich den Bereich der vorherigen Kerze, um eine Neuberechnung der Datenreihen zu vermeiden.
- Blockiert doppelte Orders auf derselben Kerze, indem die Öffnungszeit des Balkens verfolgt wird, der den Handel ausgelöst hat.
- Die Diagrammausgabe zeichnet die abonnierten Kerzen und die Strategiegeschäfte zur schnellen visuellen Überprüfung.

## Unterschiede zur MQL-Version

- Take-Profit- und Stop-Loss-Level wurden im ursprünglichen EA nicht richtig festgelegt (der MQL-Code hat Werte an die falschen Parameter übergeben)
. Der Port StockSharp behält daher das Verhalten bei, dass er ohne Schutzbefehle ausgeführt wird.
- Bei der Spread-Verarbeitung werden jetzt Geld-/Briefkurse in Echtzeit überprüft, sofern verfügbar, wodurch ein anpassungsfähigerer Puffer bereitgestellt wird.

## Anforderungen

- StockSharp API mit Zugriff auf Kerzendaten für das ausgewählte Instrument.
- Kurse der Stufe 1 sind optional, verbessern jedoch die Spread-Erkennung.
