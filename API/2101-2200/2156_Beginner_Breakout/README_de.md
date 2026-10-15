# Anfänger-Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Verwendet die höchsten und niedrigsten Preise der letzten `Period` Kerzen, um einen Kanal zu bilden. Wenn der Schlusskurs die obere Grenze annähert, geht die Strategie long. Wenn der Schlusskurs die untere Grenze annähert, geht sie short.

## Einstiegsregeln
- **Long**: Close >= highest - (highest - lowest) * `ShiftPercent` / 100 und der Trend ist noch nicht aufwärts.
- **Short**: Close <= lowest + (highest - lowest) * `ShiftPercent` / 100 und der Trend ist noch nicht abwärts.

## Ausstiegsregeln
- Ein entgegengesetztes Signal schließt die aktuelle Position und öffnet eine neue in der anderen Richtung.

## Parameter
- `Period` – Balken für die Kanalberechnung zurückschauen.
- `ShiftPercent` – prozentualer Versatz von den Kanalgrenzen.
- `CandleType` – Zeitrahmen der Arbeitskerzen.
- `Volume` – Handelsvolumen.
- `StopLoss` – Stop-Loss in Preiseinheiten.
- `TakeProfit` – Take-Profit in Preiseinheiten.

## Indikatoren
- Highest
- Lowest
