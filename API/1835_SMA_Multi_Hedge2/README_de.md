# SMA Multi Hedge2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt ein Basiswertpapier und sichert es mit einem korrelierten Instrument ab. Die Trendrichtung wird durch einen Simple Moving Average (SMA) bestimmt. Wenn die Korrelation zwischen dem Basis- und dem Absicherungssymbol einen Schwellenwert überschreitet, werden beide Instrumente gehandelt, um ein marktneutrales Paar zu bilden.

## Funktionsweise

1. Den Trend des Basissymbols mit einem SMA konfigurierbarer Länge berechnen.
2. Die Korrelation zwischen Basis- und Absicherungssymbol anhand der Differenz zwischen dem Preis und seinem eigenen SMA messen.
3. Wenn die Korrelation das erwartete Niveau erreicht, Positionen in beiden Instrumenten eröffnen. Die Absicherungsrichtung kann je nach Konfiguration der Basis folgen oder entgegengesetzt sein.
4. Positionen werden automatisch geschlossen, wenn der kombinierte Gewinn den Zielwert erreicht.

## Parameter

- `SmaPeriod` — Periode des SMA zur Trenddetektion. Standard ist 20.
- `CorrelationPeriod` — Anzahl der Stichproben zur Korrelationsbewertung. Standard ist 20.
- `ExpectedCorrelation` — minimale absolute Korrelation zur Aktivierung der Absicherung. Standard ist 0.8.
- `ProfitTarget` — Gesamtgewinnziel in Geldeinheiten. Standard ist 30.
- `CandleType` — Datentyp für Kerzenabonnement. Standard ist 1-Minuten-Zeitrahmen.
- `FollowBase` — wenn wahr, handelt die Absicherung bei positiver Korrelation in dieselbe Richtung.

## Indikatoren

- SMA
- Korrelation (benutzerdefinierte Berechnung)

## Hinweise

Dies ist ein vereinfachter Port der ursprünglichen MQL-Strategie. Risiko- und Geldmanagement sollten vor dem Live-Trading angepasst werden.

