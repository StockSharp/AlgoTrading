# AFL Winner V2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Diese Beispielstrategie repliziert die Logik des AFL Winner V2-Indikators mithilfe der High-Level-API von StockSharp. Der Indikator wird durch einen Stochastik-Oszillator angenähert und Signale werden aus seiner relativen Position und vordefinierten Schwellenwerten abgeleitet.

## Strategielogik

- Verwendet einen `StochasticOscillator`, um das AFL-Winner-Verhalten zu emulieren.
- Eröffnet eine Long-Position, wenn der Oszillator starken Aufwärts-Momentum anzeigt.
- Eröffnet eine Short-Position, wenn der Oszillator starken Abwärts-Momentum signalisiert.
- Schließt Longs, wenn der Farbzustand unter die neutrale Zone fällt.
- Schließt Shorts, wenn der Farbzustand über die neutrale Zone steigt.
- Parameter ermöglichen die Optimierung von Oszillator-Perioden und Schwellenwerten.

## Parameter

| Parameter   | Beschreibung                                        |
|-------------|-----------------------------------------------------|
| `KPeriod`   | %K-Periode des Stochastik-Oszillators.              |
| `DPeriod`   | %D-Periode des Stochastik-Oszillators.              |
| `HighLevel` | Oberer Schwellenwert für bullische Signale.         |
| `LowLevel`  | Unterer Schwellenwert für bärische Signale.         |

## Dateien

- `CS/AflWinnerV2Strategy.cs` – Kernimplementierung der Strategie.

## Hinweise

Die Strategie arbeitet nur mit abgeschlossenen Kerzen und verwendet automatischen Positionsschutz, um unbeabsichtigtes Exposure zu vermeiden.
