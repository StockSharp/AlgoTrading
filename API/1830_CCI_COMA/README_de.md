# CCI COMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Verwendet den Commodity Channel Index und gleitende Durchschnitte mehrerer Zeitrahmen, um dem vorherrschenden Trend zu folgen.

## Details

- **Daten**: Kurskerzen aus mehreren Zeitrahmen.
- **Einstieg**: Long, wenn CCI über null liegt, RSI über 50, die Kerze über dem Eröffnungskurs schließt und alle überwachten Zeitrahmen einen Aufwärtstrend zeigen; Short bei gegenteiligen Bedingungen.
- **Ausstieg**: Position schließt beim entgegengesetzten Signal.
- **Instrumente**: Beliebige Instrumente.
- **Risiko**: Kein expliziter Stop-Loss oder Take-Profit.
