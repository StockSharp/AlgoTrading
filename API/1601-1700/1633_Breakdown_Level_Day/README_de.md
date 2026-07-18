# Tagesbereichs-Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie platziert ausstehende Stop-Orders ober- und unterhalb der Intraday-Spanne zu einer festgelegten Tageszeit. Ziel ist es, Ausbrüche zu erfassen, wenn der Preis das frühe Sitzungshoch oder -tief überschreitet. Optionale Stop-Loss-, Take-Profit-, Breakeven- und Trailing-Stop-Regeln verwalten die offene Position.

## Details

- **Einstieg**: Zum Zeitpunkt `OrderTime` wird ein Buy-Stop oberhalb des Tageshochs plus `Delta` Ticks und ein Sell-Stop unterhalb des Tagestiefs minus `Delta` Ticks platziert.
- **Ausstieg**: Stop-Loss- und Take-Profit-Orders werden zusammen mit der Ausbruchs-Order platziert. Breakeven und Trailing-Stop können den Schutz-Stop aktualisieren.
- **Indikatoren**: Keine.
- **Zeitrahmen**: Standard 1-Minuten-Kerzen.
- **Risiko**: Die Positionsgröße wird aus der `Volume`-Eigenschaft der Strategie übernommen.

## Parameter

- `OrderTime` — Tageszeit, zu der ausstehende Orders eingereicht werden.
- `Delta` — Abstand von den Bereichsgrenzen in Ticks.
- `StopLoss` — Schutz-Stop-Abstand in Ticks.
- `TakeProfit` — Gewinnziel-Abstand in Ticks.
- `BreakEven` — Stop nach diesem Gewinn in Ticks auf den Einstieg verschieben.
- `Trailing` — Trailing-Stop-Abstand in Ticks.
- `CandleType` — Kerzentyp für Berechnungen.
