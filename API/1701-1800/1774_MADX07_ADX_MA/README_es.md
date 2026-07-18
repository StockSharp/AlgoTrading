# Estrategia MADX-07 ADX MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia ha sido convertida del asesor experto MQL4 MADX-07. Opera en velas H4 y combina dos medias móviles con el Índice de Movimiento Direccional Promedio (ADX) como filtros.

## Lógica

- Entrada larga: Precio por encima de la MA lenta, MA rápida por encima de la MA lenta, precio al menos `MaDifference` puntos por encima de la MA rápida durante las dos últimas velas, ADX subiendo por encima de `AdxMainLevel` con +DI subiendo y -DI bajando.
- Entrada corta: Condiciones espejo.
- La posición se cierra cuando el beneficio en puntos alcanza `CloseProfit` o cuando se ejecuta una orden limitada a una distancia de `TakeProfit`.

## Parámetros

- `BigMaPeriod` (25) – período de la MA lenta.
- `BigMaType` – tipo de la MA lenta.
- `SmallMaPeriod` (5) – período de la MA rápida.
- `SmallMaType` – tipo de la MA rápida.
- `MaDifference` (5) – distancia mínima entre el precio y la MA rápida en puntos.
- `AdxPeriod` (11) – período de cálculo del ADX.
- `AdxMainLevel` (13) – valor mínimo del ADX.
- `AdxPlusLevel` (13) – valor mínimo del +DI.
- `AdxMinusLevel` (14) – valor mínimo del -DI.
- `TakeProfit` (299) – distancia del take-profit en puntos.
- `CloseProfit` (13) – beneficio en puntos para la salida anticipada.
- `Volume` (0.1) – volumen de la operación.
- `CandleType` – marco temporal de las velas (por defecto H4).
