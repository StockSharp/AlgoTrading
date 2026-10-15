# Technische Bewertungen für Multi-Timeframe-Assets (Strategie)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie aggregiert technische Bewertungen aus mehreren Zeitrahmen.
Sie vergleicht den Preis mit einem gleitenden Durchschnitt und RSI-Schwellenwerten auf 1h-, 4h- und Tageskerzen.
Eine Long-Position wird eröffnet, wenn die kombinierte Bewertung positiv ist, und eine Short-Position, wenn sie negativ ist.

## Details

- **Einstieg**: Kaufen, wenn die durchschnittliche Bewertung > 0; verkaufen, wenn die durchschnittliche Bewertung < 0.
- **Indikatoren**: SMA, RSI.
- **Zeitrahmen**: 1h, 4h, 1d.
- **Typ**: Trendfolge.
- **Stops**: Keine.
- **Richtung**: Long und Short.
- **Risiko**: Mittel.
- **Komplexität**: Mittel.
