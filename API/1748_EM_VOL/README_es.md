# Estrategia EM VOL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera rupturas en torno a niveles de soporte y resistencia basados en pivotes.
Calcula el máximo y mínimo del día anterior más un buffer ATR para formar los disparadores de entrada.
Las operaciones se colocan solo cuando el indicador ADX señala un entorno de baja volatilidad.

## Lógica

1. Calcular el pivote de la barra anterior y añadir/restar ATR para obtener resistencia y soporte.
2. Si el ADX está por debajo del umbral y el precio cierra por encima de la resistencia, entrar en posición larga.
3. Si el precio cierra por debajo del soporte, entrar en posición corta.
4. Tras la entrada, se colocan órdenes de stop de protección y take profit.
5. Un trailing stop puede ajustar el stop de protección una vez que la ganancia alcanza el nivel especificado.

## Parámetros

- `TakeProfit` — distancia del take profit en pasos de precio.
- `StopLoss` — distancia del stop loss en pasos de precio.
- `AtrPeriod` — período del indicador ATR.
- `AdxPeriod` — período del indicador ADX.
- `AdxThreshold` — valor máximo de ADX para permitir el trading.
- `TrailStart` — ganancia requerida antes de que comience el trailing stop.
- `TrailStep` — distancia del trailing stop.
- `CandleType` — marco temporal utilizado para los cálculos.

## Indicadores Utilizados

- Average True Range
- Average Directional Index
