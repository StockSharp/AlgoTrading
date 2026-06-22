# Hedge Average-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie reproduziert den "Hedge Average"-Experten aus MetaTrader. Sie vergleicht einfache gleitende Durchschnitte der Eröffnungs- und Schlusskurse über zwei Zeiträume.

## Handelslogik

- Berechnung des SMA des Eröffnungs- und Schlusskurses für `Period1` und `Period2`.
- Wenn der langperiodische Eröffnungsdurchschnitt über seinem Schlussdurchschnitt liegt **und** der kurzperiodische Eröffnungsdurchschnitt unter seinem Schlussdurchschnitt liegt, wird eine Long-Position eröffnet.
- Wenn der langperiodische Eröffnungsdurchschnitt unter seinem Schlussdurchschnitt liegt **und** der kurzperiodische Eröffnungsdurchschnitt über seinem Schlussdurchschnitt liegt, wird eine Short-Position eröffnet.
- Der Handel ist nur zwischen `StartHour` und `EndHour` erlaubt.
- Optionaler Stop-Loss und Take-Profit werden in absoluten Preiseinheiten gesetzt. Der Trailing Stop bewegt den Schutz-Stop mit dem Preis, wenn er aktiviert ist.

## Parameter

- `Period1` – Periode für die schnellen Durchschnitte.
- `Period2` – Periode für die langsamen Durchschnitte.
- `StartHour` – Tagesstunde, zu der der Handel beginnt.
- `EndHour` – Tagesstunde, zu der der Handel endet.
- `CandleType` – Kerzen-Zeitrahmen für Berechnungen.
- `TakeProfit` – Take-Profit-Abstand in Preiseinheiten.
- `StopLoss` – Stop-Loss-Abstand in Preiseinheiten.
- `UseTrailing` – Trailing Stop auf Basis des Stop-Loss-Abstands aktivieren.

## Hinweise

Die Strategie verwendet einen Einzelpositionsansatz und repliziert nicht das geldbasierte Gewinnziel aus der ursprünglichen MQL-Version.
