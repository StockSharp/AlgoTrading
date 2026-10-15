# 3MA Bunny Cross-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **ThreeMaBunnyCrossStrategy** ist eine Umsetzung des MetaTrader 4 Expertenberaters „3MA Bunny Cross“. Es werden Trendumkehrungen basierend auf dem Schnittpunkt zwischen zwei linear gewichteten gleitenden Durchschnitten (LWMAs) gehandelt, die auf der Grundlage der Schlusskurse des ausgewählten Zeitrahmens berechnet werden. Die StockSharp-Version behält die ursprüngliche Idee bei, die Position unmittelbar nach einem Crossover umzukehren, und fügt hochrangige API-Annehmlichkeiten wie Indikatorbindung und integrierten Risikoschutz hinzu.

## Ursprüngliche MQL-Beschreibung
Der Quell-Expertenberater verwendet zwei LWMAs mit den Perioden 5 und 20. Wenn der schnelle LWMA den langsamen LWMA kreuzt, schließt der Berater die entgegengesetzte Position, sofern vorhanden, und eröffnet sofort einen neuen Trade in Richtung des Crossovers. Es ist immer nur eine Position erlaubt. Das ursprüngliche Skript prüft vor dem Handel auch, ob eine Mindestanzahl an Balken und eine freie Marge vorhanden sind.

## StockSharp Implementierungsdetails
- Die Strategie abonniert Kerzen, die durch den Parameter `CandleType` definiert sind (standardmäßig 15-Minuten-Zeitrahmen) und bindet sie an zwei `LinearWeightedMovingAverage`-Indikatoren.
- Indikatorwerte werden der Verarbeitungsmethode direkt über `Bind` bereitgestellt, sodass keine manuelle Pufferbehandlung erforderlich ist.
- Die vorherigen schnellen und langsamen Werte werden zwischengespeichert, um Überschneidungen zu erkennen, wobei die gleiche Logik wie bei der MQL-Version verwendet wird (`fast`-Kreuzung über oder unter `slow`).
- Wenn es zu einem bullischen Crossover kommt und die aktuelle Position flach oder short ist, sendet die Strategie eine marktübliche Kauforder in der Größe, sowohl ein Short-Engagement zu schließen als auch eine neue Long-Position zu eröffnen (`Volume + |Position|`). Der rückläufige Crossover verhält sich bei Verkäufen symmetrisch.
- `StartProtection()` wird beim Start einmal aufgerufen, um integrierte Positionsschutzroutinen zu aktivieren.
- Die Diagrammvisualisierung zeichnet die Quellkerzen zusammen mit den beiden gleitenden Durchschnitten und den eigenen Trades der Strategie.

## Parameter
- **CandleType** – Datentyp, der die zu abonnierende Kerzenserie beschreibt (standardmäßig 15-Minuten-Zeitrahmen).
- **FastPeriod** – Zeitraum der schnellen LWMA. Standard: 5. Optimierbar.
- **SlowPeriod** – Zeitraum der langsamen LWMA. Standard: 20. Optimierbar.

## Indikatoren
- `LinearWeightedMovingAverage` (schnell, standardmäßig Periode 5).
- `LinearWeightedMovingAverage` (langsam, standardmäßig Periode 20).

## Handelsregeln
1. Warten Sie auf eine fertige Kerze und überprüfen Sie, ob die Strategie online erstellt wurde und für den Handel zugelassen ist.
2. Erkennen Sie einen bullischen Crossover, wenn der schnelle LWMA bei der vorherigen Kerze unter oder gleich dem langsamen LWMA lag und bei der aktuellen Kerze darüber oder gleich ist. Schließen Sie in diesem Fall eine bestehende Short-Position und eröffnen Sie eine Long-Position.
3. Erkennen Sie einen bearischen Crossover, wenn der schnelle LWMA bei der vorherigen Kerze über oder gleich dem langsamen LWMA lag und bei der aktuellen Kerze darunter oder gleich diesem liegt. Schließen Sie in diesem Fall eine eventuell bestehende Long-Position und eröffnen Sie eine Short-Position.
4. Jede neue Ordergröße wird als `Volume + |Position|` berechnet, um alle ausstehenden Positionen vollständig umzukehren und sicherzustellen, dass immer nur eine Richtungsposition vorhanden ist.
