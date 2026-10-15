# Technischer Händler-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Technical Trader reimplementiert den MetaTrader Expert Advisor aus `MQL/22304/Technical_trader.mq5`, indem zwei einfache gleitende Durchschnitte mit einem adaptiven Liquiditätscluster-Detektor kombiniert werden. Die Strategie sucht nach wiederholt gehandelten Preisleveln in der Nähe des aktuellen Bid/Ask und öffnet nur dann Trades, wenn diese Cluster mit der Richtung des Schnell/Langsam-SMA-Kreuzungssignals übereinstimmen. Das Risiko wird durch preisschrittbasierte Stop-Loss- und Take-Profit-Offsets kontrolliert, die die ursprüngliche MQL-Konfiguration widerspiegeln.

## Überblick
- **Plattform:** StockSharp High-Level-Strategie-API.
- **Marktdaten:** Zeitrahmen-definierte Kerzen plus Orderbuch-Snapshots zur Ermittlung aktueller Bid/Ask-Preise.
- **Stil:** Direktionaler Ausbruch nach nahen Liquiditätsclustern.
- **Quellzuordnung:** SMA-Kreuzung, historische Close-Probenahme, Clustering-Toleranz und Order-Dimensionierung wurden vom MQL-Expert portiert.

## Handelslogik
1. Kerzen des konfigurierten Zeitrahmens abonnieren und zwei SMAs berechnen (`FastMaPeriod` und `SlowMaPeriod`).
2. Ein rollierendes Fenster (`HistoryDepth`) der aktuellsten Schlusskurse pflegen und auf drei Dezimalstellen runden, um das ursprüngliche `NormalizeDouble`-Verhalten zu emulieren.
3. Ein Histogramm der Preisvorkommen erstellen und Level klassifizieren, deren Häufigkeit `ResistanceThreshold` überschreitet.
4. Den neuesten Bid und Ask über das Orderbuch verfolgen; auf den Kerzenschlusskurs zurückgreifen, wenn keine Notierungen verfügbar sind.
5. Long-Einstiegsbedingungen:
   - Schnelle SMA liegt über der langsamen SMA.
   - Ein qualifizierter Preiscluster befindet sich direkt unterhalb des aktuellen Ask (`LevelTolerance` definiert die erlaubte Distanz).
   - Wenn die Strategie flach oder short ist, kauft sie genug Volumen, um die Short-Position zu schließen und die Long-Basisposition aufzubauen.
6. Short-Einstiegsbedingungen spiegeln die Long-Logik wider, verwenden aber Cluster direkt über dem Bid und erfordern, dass die schnelle SMA unter der langsamen SMA liegt.
7. Beim Einstieg in eine Position werden Stop-Loss- und Take-Profit-Level berechnet, indem der `PriceStep` des Wertpapiers mit `StopLossPoints` bzw. `TakeProfitPoints` multipliziert wird. Diese Offsets recreieren die `_Point`-Multiplikatoren in der MQL-Version.
8. Bei jeder abgeschlossenen Kerze werden Positionen beendet, wenn der verfolgte Bid/Ask den Stop-Loss- oder Take-Profit-Level erreicht.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|--------------|----------|
| `FastMaPeriod` | Länge der schnellen SMA, die das Kreuzungssignal antreibt. | 25 |
| `SlowMaPeriod` | Länge der langsamen SMA als Trendfilter. | 30 |
| `StopLossPoints` | Stop-Distanz in Kursschritten (`PriceStep * StopLossPoints`). | 30 |
| `TakeProfitPoints` | Gewinnziel in Kursschritten (`PriceStep * TakeProfitPoints`). | 100 |
| `ResistanceThreshold` | Mindestanzahl von Vorkommen, damit ein Preislevel als Liquiditätscluster behandelt wird. | 15 |
| `HistoryDepth` | Anzahl der gespeicherten aktuellen Kerzen für die Cluster-Erkennung (für Gold-Paare auf 100 setzen, wie im Original-EA). | 500 |
| `LevelTolerance` | Maximal erlaubte Distanz zwischen dem aktuellen Bid/Ask und einem Cluster-Level. | 0.0005 |
| `CandleType` | Von der Strategie verarbeitete Kerzenserie (Zeitrahmen oder benutzerdefinierter Typ). | 1-Minuten-Zeitrahmen |

## Implementierungshinweise
- Die Orderbuch-Abonnierung wird verwendet, um aktuelle beste Bid/Ask-Preise zu erfassen und die tick-basierte Ausführung im MQL-Expert zu matching.
- Die Cluster-Berechnung vermeidet LINQ und speichert Ergebnisse in wiederverwendbaren Puffern, um die StockSharp-Konvertierungsrichtlinien einzuhalten.
- Stop- und Take-Profit-Ziele werden intern verwaltet, da StockSharp-Strategien synthetische Orders statt broker-seitiger ausstehender Orders ausführen.
- Charting-Helper zeichnen Kerzen, beide SMAs und ausgeführte Trades zur visuellen Überprüfung während des Testens.

## Verwendungstipps
- `HistoryDepth` erhöhen, wenn auf höheren Zeitrahmen gearbeitet wird, um eine aussagekräftige Stichprobengröße für das Level-Clustering beizubehalten.
- `LevelTolerance` auf Instrumenten mit kleinen Tick-Größen enger stellen, um unzusammenhängende Cluster zu vermeiden.
- `ResistanceThreshold` auf illiquiden Märkten senken, wo weniger Wiederholungen erwartet werden.
- Der Standard-Volumenparameter der Basisklasse `Strategy` steuert die Ordergröße; im Hosting-Environment anpassen oder vor dem Starten der Strategie überschreiben.
