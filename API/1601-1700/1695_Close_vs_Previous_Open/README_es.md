# Estrategia de Cierre vs Apertura Anterior
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia compara el cierre de la última vela finalizada con la apertura de la vela anterior.
Abre una posición larga cuando el último cierre está por encima de la apertura anterior y una posición corta cuando el último cierre está por debajo de la apertura anterior.

## Reglas de entrada
- **Long**: El cierre de la vela completada más reciente es mayor que la apertura de la vela anterior.
- **Short**: El cierre de la vela completada más reciente es menor que la apertura de la vela anterior.

## Gestión de riesgos
- Stop loss y take profit opcionales medidos en puntos.
- Trailing del stop loss opcional.

## Parámetros
- `Volume` – volumen de la orden.
- `UseStopLoss` – habilitar stop loss.
- `StopLoss` – distancia del stop loss en puntos.
- `UseTakeProfit` – habilitar take profit.
- `TakeProfit` – distancia del take profit en puntos.
- `UseTrailingStop` – seguir el stop loss con el movimiento del precio.
- `CandleType` – serie de velas para los cálculos.

## Notas
- Opera únicamente en velas completamente formadas.
- Invierte la posición cuando aparece la señal opuesta.
