# Estrategia CCI MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Combina cruces del CCI con un filtro MACD y bandas EMA/ATR para operar en la dirección de la tendencia.

## Detalles

- **Datos**: Velas de precio.
- **Entrada**: Largo cuando el CCI cruza por encima de cero, MACD por encima de cero, precio por encima de EMA125 y EMA750 pero por debajo de la banda ATR superior; corto cuando lo contrario.
- **Salida**: La posición se cierra con la señal opuesta.
- **Instrumentos**: Cualquier instrumento.
- **Riesgo**: Sin stop loss ni take profit.
