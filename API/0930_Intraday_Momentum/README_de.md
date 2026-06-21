# Intraday-Momentum-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Handelt innerhalb einer festgelegten Sitzung mit EMA-Kreuzung, RSI-Filter und VWAP-Bestätigung. Geht long, wenn die schnelle EMA über die langsame EMA kreuzt, der RSI unter dem überkauften Niveau liegt und der Kurs über dem VWAP notiert. Shorts bei entgegengesetzten Bedingungen. Wendet feste Stop-Loss- und Take-Profit-Prozentsätze an und schließt alle Positionen am Ende der Sitzung.

## Parameter

- **EmaFastLength**: Länge der schnellen EMA.
- **EmaSlowLength**: Länge der langsamen EMA.
- **RsiLength**: RSI-Periode.
- **RsiOverbought**: RSI-Überkauft-Niveau.
- **RsiOversold**: RSI-Überverkauft-Niveau.
- **StopLossPerc**: Stop-Loss-Prozentsatz.
- **TakeProfitPerc**: Take-Profit-Prozentsatz.
- **StartHour**: Startstunde der Sitzung.
- **StartMinute**: Startminute der Sitzung.
- **EndHour**: Endstunde der Sitzung.
- **EndMinute**: Endminute der Sitzung.
- **CandleType**: Kerzentyp.

