# Martingail-Expertenstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Martingail Expert ist eine trendfolgende Martingal-Strategie, die auf dem Stochastic-Oszillator basiert, um neue Handelssequenzen zu timen. Sobald der Indikator eine Richtung generiert, startet die Strategie eine Leiter von Marktaufträgen und verwaltet das Risiko mithilfe eines dynamischen Gewinnziels und eines geometrischen Positionsgrößenschemas.

## Handelslogik
- Berechnen Sie einen Stochastic-Oszillator für die konfigurierte Kerzenreihe. Die aktuellsten Endwerte von %K und %D werden zur Entscheidungsfindung zwischengespeichert.
- Starten Sie eine neue lange Sequenz, wenn `%K (previous) > %D (previous)` und `%D (previous)` über dem Schwellenwert `BuyLevel` liegen.
- Starten Sie eine neue kurze Sequenz, wenn `%K (previous) < %D (previous)` und `%D (previous)` unter dem Schwellenwert `SellLevel` liegen.
- Nach Eingabe einer Sequenz fügt jede günstige Preisbewegung von `ProfitFactor × openOrders` Pips eine neue Position mit dem Basisvolumen hinzu.
- Jede negative Bewegung von `StepPoints` Pips multipliziert das zuletzt gefüllte Volumen mit `Multiplier` und sendet eine durchschnittliche Order in die gleiche Richtung.

## Ausgangsregeln
- Schließen Sie die gesamte Position, sobald der letzte Füllpreis ein dynamisches Gewinnziel bei `ProfitFactor × openOrders` Pips in die günstige Richtung erreicht.
- Setzen Sie den Martingal-Status zurück, wenn die aggregierte Positionsgröße auf Null zurückkehrt.

## Risikomanagement
Die Martingal-Progression erhöht das Engagement schnell, wenn sich der Preis gegen die Position bewegt. Passen Sie `Multiplier`, `StepPoints` und `ProfitFactor` sorgfältig an, um sie an die Kontogröße und die Instrumentenvolatilität anzupassen.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `Volume` | Basis-Market-Order-Volumen, das für den ersten Trade und alle vorteilhaften Add-Ons verwendet wird. |
| `Multiplier` | Faktor, der bei der Mittelung bei ungünstigen Bewegungen auf das zuletzt ausgeführte Volumen angewendet wird. |
| `StepPoints` | Entfernung in Punkten, die eine Martingal-Mittelungsreihenfolge auslöst. |
| `ProfitFactor` | Gewinnziel pro offener Order, ausgedrückt in Punkten. Die tatsächliche Entfernung beträgt `ProfitFactor × number_of_orders`. |
| `KPeriod` | Lookback-Länge für die %K-Zeile. |
| `DPeriod` | Glättungslänge für die %D-Linie. |
| `Slowing` | Zusätzliche Glättung wurde auf %K vor dem Vergleich mit %D angewendet. |
| `BuyLevel` | Mindestwert %D erforderlich, um eine neue lange Sequenz zu ermöglichen. |
| `SellLevel` | Maximaler %D-Wert erforderlich, um eine neue kurze Sequenz zu ermöglichen. |
| `CandleType` | Für Berechnungen verwendete Kerzenserie (Standard: 5-Minuten-Zeitrahmen). |

## Notizen
- Funktioniert am besten bei flüssigen FX-Paaren, bei denen Pip-Größe und Volumenschritt eine granulare Skalierung ermöglichen.
- Erfordert ausreichend Spielraum, um mehreren Martingalschritten standzuhalten.
