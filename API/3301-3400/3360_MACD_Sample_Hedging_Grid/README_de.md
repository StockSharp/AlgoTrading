# MACD Beispiel einer Hedging-Grid-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine StockSharp-Portierung des Expertenberaters MetaTrader „MACD Sample Hedging Grid“. Es kombiniert einen kurzfristigen MACD-Crossover, einen lokalen EMA-Steigungsfilter und Bestätigungen für höhere Zeitrahmen. Wenn die Bedingungen übereinstimmen, erstellt die Strategie ein Positionsraster in der erkannten Richtung und skaliert die Handelsgröße um einen konfigurierbaren Exponenten.

## Marktlogik
- **Basiszeitrahmen:** konfigurierbar (Standard 5-Minuten-Kerzen).
- **Trendfilter:** ein EMA (Standard 26 Perioden) muss für Long-Trades nach oben und für Short-Trades nach unten verlaufen.
- **MACD-Trigger:** Die schnelle MACD-Linie muss die Signallinie im Basiszeitrahmen kreuzen und dabei einen minimalen absoluten Wert (ausgedrückt in Preisschritten) überschreiten.
- **Momentum-Bestätigung:** Der absolute Abstand zwischen dem Momentum und dem neutralen 100-Niveau in einem höheren Zeitrahmen muss separate Schwellenwerte für Long- und Short-Positionen überschreiten. Die letzten drei Kerzen mit höherem Zeitrahmen werden überprüft und reproduzieren das ursprüngliche EA-Verhalten.
- **Langfristige Bestätigung:** Ein MACD, der auf einem langen Zeitrahmen (standardmäßig monatlich) berechnet wird, muss mit der Handelsrichtung übereinstimmen (MACD oben Signal für bullisches, unten für bärisches Umfeld).

Sobald ein Signal ausgelöst wird, startet die Strategie entweder ein neues Raster in dieser Richtung oder ergänzt das bestehende Raster, solange die maximale Anzahl an Einträgen noch nicht erreicht ist.

## Positionsmanagement
- **Rastergröße:** Jeder zusätzliche Eintrag multipliziert das anfängliche Volumen mit dem `LotExponent` (Standard 1,44). Die Positionsgröße wird zurückgesetzt, wenn sich die Richtung ändert oder die Position geschlossen wird.
- **Risikokontrollen:** Optionale Take-Profit- und Stop-Loss-Abstände werden in Preisschritten in StockSharp Schutzaufträge übersetzt.
- **Richtungsänderung:** Immer wenn ein entgegengesetztes Signal eintrifft, wird die aktuelle Belichtung abgeflacht, bevor das Raster in die neue Richtung geöffnet wird.

## Parameter
| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `CandleType` | Hauptzeitrahmen für MACD- und EMA-Berechnungen. | Zeitrahmen von 5 Minuten |
| `MomentumCandleType` | Höherer Zeitrahmen, der die Dynamik bestätigt. | 30-minütiger Zeitrahmen |
| `TrendCandleType` | Für den Trendfilter MACD wird ein langer Zeitraum verwendet. | 30-tägiger Zeitrahmen |
| `FastMaPeriod` | Schnelle Länge von EMA innerhalb von MACD. | 12 |
| `SlowMaPeriod` | Langsame Länge von EMA innerhalb von MACD. | 26 |
| `SignalPeriod` | Signallänge SMA für MACD. | 9 |
| `TrendMaPeriod` | EMA Länge für den lokalen Trendfilter. | 26 |
| `MomentumPeriod` | Länge des Momentum-Indikators (höherer Zeitrahmen). | 14 |
| `MacdOpenLevel` | Mindestabsolutes MACD-Niveau (in Preisschritten), das für einen Handel erforderlich ist. | 3 |
| `MomentumBuyThreshold` | Minimaler absoluter Impulsabstand von 100 für Long-Positionen. | 0,3 |
| `MomentumSellThreshold` | Minimaler absoluter Impulsabstand von 100 für Shorts. | 0,3 |
| `MaxTrades` | Maximale Anzahl an Rastereinträgen pro Richtung. | 10 |
| `LotExponent` | Multiplikator für jeden weiteren Rastereintrag. | 1,44 |
| `StopLossSteps` | Stop-Loss-Distanz gemessen in Preisschritten. | 20 |
| `TakeProfitSteps` | Take-Profit-Distanz gemessen in Preisschritten. | 50 |

## Notizen
- Der ursprüngliche EA enthielt auch geldbasierte Trailing-, Break-Even-Bewegungen und Kontokapitalstopps. Diese Funktionen erfordern maklerspezifische Portfoliodaten und eine manuelle Auftragsverwaltung; Sie sind in dieser übergeordneten StockSharp-Konvertierung nicht implementiert.
- Kerzenabonnements, Indikatorbindungen und Handelsausführung folgen der von StockSharp empfohlenen allgemeinen Verwendung von API.
- Stellen Sie sicher, dass die ausgewählten Instrumente die konfigurierten Kerzentypen unterstützen und dass historische Daten für alle referenzierten Zeitrahmen verfügbar sind.
