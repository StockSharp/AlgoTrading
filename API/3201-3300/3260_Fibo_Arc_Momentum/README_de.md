# Fibo Arc Momentum-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie ist ein StockSharp-Port des MetaTrader-Expertenberaters "FiboArc" (Ordner `MQL/24924`). Das originale EA kombiniert mehrere Momentum-Filter mit Fibonacci-Arc-Ausbrüchen. Die StockSharp-Implementierung behält dieselbe Idee bei und passt sie an die High-Level-Kerzen-API an:

* Zwei linear gewichtete gleitende Durchschnitte (`FastMaPeriod`, `SlowMaPeriod`) definieren die Trendrichtung.
* Ein Momentum-Oszillator, gemessen am neutralen 100-Niveau, filtert schwache Setups heraus.
* Ein MACD-Histogramm bestätigt die Trendstärke und erkennt frische Kreuzungen.
* Ein vereinfachter Fibonacci-Arc wird auf jedem Balken unter Verwendung der Eröffnungspreise zweier durch `TrendAnchorLength` und `ArcAnchorLength` ausgewählter Ankerkerzen rekonstruiert. Ein Ausbruch durch dieses dynamische Niveau ersetzt die objektbasierten Prüfungen der MetaTrader-Version.

Die Strategie funktioniert mit jedem Symbol/Zeitrahmen-Paar, das von StockSharp unterstützt wird. Alle Berechnungen laufen auf vollständig fertigen Kerzen, um das EA-Verhalten zu spiegeln und Lookahead-Bias zu vermeiden.

## Indikatoren und Datenfluss

Die Strategie abonniert einen einzelnen Kerzenstrom, der durch `CandleType` konfiguriert wird. Jede neue fertige Kerze wird über `SubscribeCandles(...).BindEx(...)` in folgende Indikatoren eingespeist:

| Indikator | Zweck | Standardeinstellungen |
|-----------|---------|------------------|
| LinearWeightedMovingAverage (schnell) | Kurzfristiger Trend und Einstiegs-Timing | `FastMaPeriod = 6`, typischer Preis |
| LinearWeightedMovingAverage (langsam) | Übergeordneter Trendfilter | `SlowMaPeriod = 85`, typischer Preis |
| Momentum | Abstand von 100 dient zur Bestätigung starker Bewegungen | `MomentumPeriod = 14` |
| MovingAverageConvergenceDivergenceSignal | Bestätigt den Trend und erkennt Kreuzungen | `MacdFastPeriod = 12`, `MacdSlowPeriod = 26`, `MacdSignalPeriod = 9` |

Indikatorausgaben werden als `IIndicatorValue`-Instanzen empfangen; nur Endwerte werden verarbeitet.

## Fibonacci-Arc-Rekonstruktion

MetaTrader zeichnet ein echtes Arc-Objekt und liest dessen Werte mit `ObjectGetValueByShift`. StockSharp verwendet keine Chart-Objekte, daher wird der Arc numerisch emuliert:

1. Die Strategie führt eine fortlaufende Liste fertiger Kerzen (`_history`).
2. `TrendAnchorLength` wählt den Index des Basisankers, und `ArcAnchorLength` wählt den zweiten Anker.
3. Das Arc-Niveau für die aktuelle Kerze wird als lineare Interpolation zwischen den Ankereröffnungen unter Verwendung von `FibonacciRatio` (Standard 0.618) berechnet.
4. Für die Ausbruchserkennung wird die vorherige Kerzeneröffnung mit dem vorherigen Arc-Niveau und die aktuelle Kerzeneröffnung mit dem neu berechneten Niveau verglichen. Ein Kreuz von unten (`fibCrossUp`) oder von oben (`fibCrossDown`) recreiert die ursprünglichen EA-Prüfungen.

## Handelsregeln

### Long-Einstiege

Eine Long-Position wird eröffnet, wenn alle folgenden Bedingungen erfüllt sind:

1. Der vorherige Balken öffnete unterhalb des vorherigen Arc-Niveaus und der aktuelle Balken öffnet oberhalb des neuen Niveaus (`fibCrossUp`).
2. Die schnelle LWMA liegt über der langsamen LWMA (`bullishTrend`).
3. Der absolute Abstand zwischen Momentum und 100 beträgt mindestens `MomentumThreshold`.
4. Die MACD-Hauptlinie liegt über ihrer Signallinie oder hat gerade nach oben gekreuzt (`macdAboveSignal` oder `macdCrossUp`).
5. Die aktuelle Positionsgröße ist kleiner oder gleich null (kein bestehendes Long-Exposure).

Die Strategie kauft `Volume` plus den absoluten Wert jedes offenen Short-Exposures, um flach-zu-Long-Übergänge sicherzustellen.

### Short-Einstiege

Short-Trades spiegeln die Long-Logik:

1. `fibCrossDown` bestätigt einen Ausbruch nach unten.
2. Die schnelle LWMA liegt unter der langsamen LWMA.
3. Der Momentum-Abstand übersteigt `MomentumThreshold`.
4. MACD liegt unter seiner Signallinie oder kreuzt nach unten.
5. Kein bestehendes Long-Exposure verbleibt.

### Ausstiege

Positionen werden geschlossen, wenn eines der folgenden eintritt:

* Trend- oder MACD-Bedingungen kippen gegen den Trade.
* Das entgegengesetzte Fibonacci-Ausbruchssignal erscheint.
* Das adaptive Stop-Loss- oder Take-Profit-Niveau wird berührt.

Alle Ausstiege werden mit Marktorders ausgeführt, um die Konsistenz mit der MetaTrader-Version zu wahren.

## Risikomanagement

Das originale EA bot geldbasierte Stops, Trailing-Logik und Break-Even-Schutz. Die StockSharp-Strategie behält dieselben Funktionen mit transparenten Parametern:

* `StopLossDistance` und `TakeProfitDistance` definieren feste Abstände in Preiseinheiten vom ausgeführten Preis.
* `EnableBreakEven`, `BreakEvenTrigger` und `BreakEvenOffset` steuern das Verhalten beim Verschieben zum Break-Even.
* `EnableTrailing`, `TrailingTrigger` und `TrailingDistance` implementieren einen kerzenbasiserten Trailing Stop.

## Parameter

| Name | Beschreibung |
|------|-------------|
| `CandleType` | Zeitrahmen (und Aggregationstyp) für alle Berechnungen. |
| `FastMaPeriod`, `SlowMaPeriod` | Trenddefinierende LWMA-Längen. |
| `MomentumPeriod`, `MomentumThreshold` | Momentum-Filtereinstellungen. |
| `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` | MACD-Konfiguration. |
| `TrendAnchorLength`, `ArcAnchorLength`, `FibonacciRatio` | Fibonacci-Arc-Rekonstruktionssteuerungen. |
| `StopLossDistance`, `TakeProfitDistance` | Anfangliche Stop- und Zielabstände (absolute Preiseinheiten). |
| `EnableBreakEven`, `BreakEvenTrigger`, `BreakEvenOffset` | Break-Even-Logik. |
| `EnableTrailing`, `TrailingTrigger`, `TrailingDistance` | Trailing-Stop-Konfiguration. |

## Verwendung

1. Fügen Sie die Strategie einem Wertpapier hinzu und setzen Sie `Volume` entsprechend der gewünschten Positionsgröße.
2. Passen Sie optional den Zeitrahmen, die gleitenden Durchschnittslängen und Fibonacci-Einstellungen an den Zielmarkt an.
3. Starten Sie die Strategie. Alle Entscheidungen basieren auf fertigen Kerzen; Intrabar-Ausführung ist nicht erforderlich.
4. Überprüfen Sie die integrierten Charting-Helfer für die schnellen/langsamen LWMA- und MACD-Panels, wenn der Host Visualisierung unterstützt.
