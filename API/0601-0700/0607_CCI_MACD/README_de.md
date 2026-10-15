# CCI MACD-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kombiniert CCI-Kreuzungen mit einem MACD-Filter und EMA/ATR-Bändern, um in Trendrichtung zu handeln.

## Details

- **Daten**: Preiskerzen.
- **Einstieg**: Long, wenn CCI null von unten kreuzt, MACD über null, Preis über EMA125 und EMA750, aber unter dem oberen ATR-Band; Short bei umgekehrten Bedingungen.
- **Ausstieg**: Position schließt bei entgegengesetztem Signal.
- **Instrumente**: Beliebige Instrumente.
- **Risiko**: Kein Stop-Loss oder Take-Profit.
