# Einfache XAUUSD-Strategie mit 20 Gewinn und 100 Verlust
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie eröffnet eine Long-Position, wenn keine Position gehalten wird und beide Abkühlungs-Timer inaktiv sind.
Die Position wird geschlossen, sobald der unrealisierte Gewinn $20 erreicht oder der Verlust $100 übersteigt.
Nach einem profitablen Ausstieg wartet die Strategie 15 Minuten vor dem erneuten Einstieg, nach einem Verlusteinstieg 12 Stunden.

## Parameter

- `ProfitTarget` – Gewinnziel in USD.
- `LossLimit` – maximaler Verlust in USD.
- `TradeCooldown` – Wartezeit nach einem Verlust.
- `EntryCooldown` – Wartezeit nach einem Gewinn.
- `CandleType` – Kerzen-Zeitrahmen.
