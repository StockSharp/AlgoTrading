# Figurelli Series-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie konvertiert den MetaTrader5-Experten "Exp_FigurelliSeries" nach StockSharp. Sie verwendet einen benutzerdefinierten Figurelli Series-Indikator, der die Differenz zwischen der Anzahl der gleitenden Durchschnitte ober- und unterhalb des aktuellen Preises misst. Der Handel findet einmal täglich zu einer benutzerdefinierten Startzeit statt, und alle Positionen werden zu einer Stoppzeit geschlossen.

## Indikator
Der Figurelli Series-Indikator erstellt eine Kette von exponentiellen gleitenden Durchschnitten, beginnend bei *Start Period* und mit *Step* für *Total* Durchschnitte inkrementierend. Für jeden Balken zählt er, wie viele Durchschnitte ober- und unterhalb des Schlusskurses liegen. Der Indikatorwert ist `bids - asks`, wobei `bids` die Anzahl der Durchschnitte unterhalb des Preises und `asks` die Anzahl der Durchschnitte oberhalb des Preises ist.

## Handelsregeln
- Zum Zeitpunkt `Start Hour:Start Minute`:
  - Kaufen, wenn der Indikatorwert positiv ist und keine Long-Position vorhanden ist.
  - Verkaufen, wenn der Indikatorwert negativ ist und keine Short-Position vorhanden ist.
- Ab `Stop Hour:Stop Minute` werden alle offenen Positionen geschlossen.
- Es werden nur abgeschlossene Kerzen des ausgewählten `Candle Type` verwendet.

## Parameter
- `StartPeriod` – anfänglicher Perioden des gleitenden Durchschnitts.
- `Step` – Periodeninkrement zwischen den Durchschnitten.
- `Total` – Anzahl der gleitenden Durchschnitte.
- `StartHour` / `StartMinute` – Zeitpunkt, zu dem Einstiege möglich sind.
- `StopHour` / `StopMinute` – Zeitpunkt zum Schließen aller Positionen.
- `CandleType` – Kerzentyp für Berechnungen.
