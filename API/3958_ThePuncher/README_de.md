# Die Puncher-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Puncher-Strategie ist ein Impulsumkehrsystem, das aus dem ursprünglichen MetaTrader 4-Expertenberater „The Puncher von L. Bigger“ abgeleitet wurde. Es kombiniert einen langsamen Stochastic-Oszillator mit einem klassischen RSI-Filter, um extrem überkaufte und überverkaufte Bedingungen zu handeln. Wenn beide Oszillatoren übereinstimmen, dass der Markt erweitert wird, sucht die Strategie nach einer Umkehr am Ende der Kerze und gibt eine Marktorder in die entgegengesetzte Richtung ein.

## Handelslogik
- **Kauf-Setup:** Wird ausgelöst, wenn die Signallinie Stochastic und RSI gleichzeitig unter den überverkauften Wert fallen. Die bestehende Short-Position (falls vorhanden) wird zuerst geschlossen und dann eine neue Long-Position eröffnet.
- **Verkaufs-Setup:** Wird ausgelöst, wenn beide Oszillatoren über das überkaufte Niveau steigen. Alle offenen Long-Positionen werden liquidiert, bevor eine neue Short-Position platziert wird.
- **Ausstiegsregeln:** Positionen werden durch entgegengesetzte Signale oder durch Schutzregeln (Stop-Loss, Take-Profit, Break-Even und Trailing Stop) geschlossen.

Die Strategie verarbeitet nur abgeschlossene Kerzen aus dem ausgewählten Zeitrahmen, um Intra-Bar-Rauschen zu vermeiden, und reproduziert das „Trade at Bar Close“-Verhalten der Quelle EA.

## Risikomanagement
- **Stop-Loss / Take-Profit:** Optionale feste Distanzen, gemessen in Pips. Wenn deaktiviert (Null), wird der entsprechende Schutz ignoriert.
- **Break-Even:** Verschiebt den Stop auf den Einstiegspreis, nachdem der Trade den angeforderten Gewinnpuffer angesammelt hat.
- **Trailing Stop:** Folgt dem Preis mit einem konfigurierbaren Abstand und einem minimalen Schritt, sodass der Stop erst dann angezogen wird, wenn der Preis ausreichend gestiegen ist.
- **Volumen:** Aufträge verwenden einen festen Volumenparameter, der die Losgrößeneingabe der MT4-Version widerspiegelt.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `OrderVolume` | Handelsvolumen für Neuzugänge. | `1` |
| `StochasticLength` | Lookback-Länge des Stochastic-Oszillators (%K). | `100` |
| `StochasticSignalPeriod` | Glättungszeitraum von %K vor dem Anlegen der Signalleitung. | `3` |
| `StochasticSmoothingPeriod` | Glättungszeitraum für die %D-Signalleitung. | `3` |
| `RsiPeriod` | Berechnungszeitraum des Filters RSI. | `14` |
| `OversoldLevel` | Von den Oszillatoren gemeinsamer Schwellenwert zur Erkennung überverkaufter Bedingungen. | `30` |
| `OverboughtLevel` | Von den Oszillatoren gemeinsamer Schwellenwert zur Erkennung überkaufter Bedingungen. | `70` |
| `StopLossPips` | Abstand des Schutzstopps (0 deaktiviert ihn). | `2000` |
| `TakeProfitPips` | Abstand des Gewinnziels (0 deaktiviert es). | `0` |
| `TrailingStopPips` | Trailing-Stop-Distanz (0 deaktiviert ihn). | `0` |
| `TrailingStepPips` | Minimale günstige Bewegung vor dem Anziehen des Trailing Stops. | `1` |
| `BreakEvenPips` | Erforderlicher Gewinn, bevor der Stop auf die Gewinnschwelle verschoben wird. | `0` |
| `CandleType` | Datentyp, der zum Erstellen von Kerzen verwendet wird. | `M15` |

## Notizen
- Die Pip-Größe wird aus dem Preisschritt oder den Dezimalstellen des Wertpapiers abgeleitet, um sicherzustellen, dass die Stop- und Trailing-Distanzen die Präzision des Instruments respektieren.
- Die Strategie eignet sich für diskretionäre Backtests, bei denen das ursprüngliche EA verwendet wurde, und kann als Grundlage für weitere Verbesserungen in StockSharp dienen.
- Audiowarnungen, E-Mails und Beschriftungen auf dem Chart aus der MT4-Version werden absichtlich weggelassen, da es sich um plattformspezifische Funktionen handelt.
