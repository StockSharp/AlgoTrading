# Cronex AC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Cronex-AC-Strategie bildet den klassischen Cronex Acceleration/Deceleration (AC) Experten-Advisor mit der StockSharp High-Level-API nach. Sie glättet den Accelerator Oscillator mit zwei aufeinanderfolgenden gleitenden Durchschnitten und reagiert, wenn die schnelle Linie die langsame Linie kreuzt. Bullische Crossover öffnen Long-Positionen und schließen Shorts, während bärische Crossover Shorts öffnen und Longs schließen.

## Handelslogik

1. Accelerator Oscillator (AO-AC)-Werte aus der ausgewählten Kerzenserie aufbauen.
2. Den AC zweimal mit dem gewählten gleitenden Durchschnittstyp glätten: Die erste Glättung erzeugt die "schnelle" Linie und die zweite Glättung erzeugt die "Signal"-Linie.
3. Die zwei Linien auf dem durch den `SignalBar`-Parameter definierten Balken auswerten. Die Strategie schaut auch einen Balken weiter zurück, um einen Crossover zu bestätigen.
4. Wenn die schnelle Linie über die Signallinie kreuzt, schließt die Strategie bestehende Short-Positionen (wenn aktiviert) und öffnet eine neue Long-Position (wenn aktiviert).
5. Wenn die schnelle Linie unter die Signallinie kreuzt, schließt die Strategie bestehende Long-Positionen (wenn aktiviert) und öffnet eine neue Short-Position (wenn aktiviert).
6. Die Positionsgröße entspricht dem konfigurierten `Volume` plus dem absoluten Wert der aktuellen Position, was Umkehrungen in einer einzigen Market-Order ermöglicht.

Die Logik spiegelt den MQL5-Experten wider, indem nur vollständig abgeschlossene Kerzen benutzt und Berechtigungen für Einstiege und Ausstiege in beiden Richtungen getrennt werden.

## Parameter

| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `SmoothingType` | `CronexMovingAverageType` | `Simple` | Gleitender-Durchschnitt-Algorithmus für den Accelerator Oscillator. Optionen: Simple, Exponential, Smoothed, Weighted. |
| `FastPeriod` | `int` | `14` | Rückblick der ersten Glättung (schnelle Linie). |
| `SlowPeriod` | `int` | `25` | Rückblick der zweiten Glättung (Signallinie). |
| `SignalBar` | `int` | `1` | Anzahl abgeschlossener Balken, die beim Lesen des Signals zurückgeblickt werden. Ein Wert von 1 repliziert das Standard-Cronex-Verhalten. |
| `CandleType` | `DataType` | `TimeFrame(8h)` | Kerzenserie für Berechnungen. |
| `EnableLongEntry` | `bool` | `true` | Long-Positionen nach einem bullischen Crossover öffnen erlauben. |
| `EnableShortEntry` | `bool` | `true` | Short-Positionen nach einem bärischen Crossover öffnen erlauben. |
| `EnableLongExit` | `bool` | `true` | Long-Positionen schließen erlauben, wenn die schnelle Linie unter die langsame Linie fällt. |
| `EnableShortExit` | `bool` | `true` | Short-Positionen schließen erlauben, wenn die schnelle Linie über die langsame Linie steigt. |
| `Volume` | `decimal` | Strategie-Standard | Ordergröße für Einstiege. Die Strategie fügt automatisch den absoluten Wert der offenen Position hinzu, um in einem Trade umzukehren. |

## Charting

Wenn ein Diagrammbereich verfügbar ist, zeichnet die Strategie:

- Quellkerzen für den ausgewählten Zeitrahmen,
- Accelerator Oscillator-Werte,
- schnelle und Signal-gleitende Durchschnitte,
- eigene Trades der Strategie zur visuellen Validierung.

## Hinweise

- Alle Berechnungen beruhen auf abgeschlossenen Kerzen (`CandleStates.Finished`), um Neuzeichnung zu vermeiden.
- Die Glättungspuffer behalten genau genug historische Werte, um den angeforderten `SignalBar`-Shift auszuwerten, passend zum ursprünglichen MQL-Experten.
- Geldmanagement-Funktionen der MQL-Version (Stop-Loss, Take-Profit, Abweichung) werden absichtlich weggelassen, damit das Positionsmanagement extern über die StockSharp-Risikokontrollen verwaltet werden kann.
