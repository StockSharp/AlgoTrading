# Estrategia Supertrend Ponderada por Volumen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia calcula un Supertrend basado en una media móvil ponderada por volumen y una banda ATR. Un segundo Supertrend se aplica al volumen para confirmar la fortaleza de la tendencia. Una posición larga se abre cuando las tendencias de volumen y precio se alinean al alza, y se cierra cuando las condiciones se revierten.

## Parámetros
- **ATR Period** – período ATR para la tendencia de precio.
- **Volume Period** – período para VWAP y la tendencia de volumen.
- **Factor** – multiplicador ATR.
- **Candle Type** – marco temporal de las velas procesadas.
