# Estrategia Psi Proc EMA MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el sistema T4 del experto MQL original `e-PSI@PROC.mq4`. Opera basándose en la alineación de múltiples medias móviles exponenciales y un filtro MACD.

## Lógica de la Estrategia

1. Calcular EMA(200), EMA(50) y EMA(10) en cada vela entrante.
2. Calcular MACD con parámetros 12, 26, 9.
3. Ir largo cuando:
   - EMA200 sube y EMA50 > EMA200.
   - EMA50 sube y EMA10 > EMA50.
   - MACD sube y está por encima de `LimitMACD`.
4. Ir corto cuando:
   - EMA200 cae y EMA50 < EMA200.
   - EMA50 cae y EMA10 < EMA50.
   - MACD cae y está por debajo de `-LimitMACD`.
5. Salir del largo cuando el precio cierra por debajo de EMA50.
6. Salir del corto cuando el precio cierra por encima de EMA50.

Se admiten protecciones opcionales de take-profit y trailing stop.

## Parámetros

| Nombre | Descripción |
| ------ | ----------- |
| `LimitMACD` | Nivel mínimo absoluto de MACD para permitir entrada. |
| `TakeProfitPoints` | Nivel de take-profit en puntos de precio. |
| `TrailStopPoints` | Nivel de trailing stop en puntos de precio. |
| `CandleType` | Marco temporal de las velas utilizadas por la estrategia. |

## Notas

- Las operaciones se abren con órdenes de mercado.
- Solo se procesan velas completadas.
- La estrategia opera sobre un único valor.
