# Estrategia de ruptura de rango
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia mide los precios más altos y más bajos dentro de las últimas `RangePeriod` velas. Cuando la vela cierra fuera de este rango y el ancho total del rango es menor que `MaxRangePoints`, la estrategia entra en la dirección de ruptura.

## Reglas de entrada
- **Largo**: Cierre de vela >= máximo más alto del rango retrospectivo Y rango en puntos <= `MaxRangePoints` Y sin posición abierta.
- **Corto**: Cierre de vela <= mínimo más bajo del rango retrospectivo Y rango en puntos <= `MaxRangePoints` Y sin posición abierta.

## Reglas de salida
- El stop loss protector y la toma de ganancias se aplican inmediatamente después de abrir la posición.
- No se utilizan reglas de salida adicionales; la posición permanece abierta hasta que la protección la cierra.

## Parámetros
- `RangePeriod` – número de velas para el cálculo más alto/más bajo.
- `MaxRangePoints` – ancho máximo del rango en puntos para permitir el comercio.
- `CandleType`: período de tiempo de las velas utilizadas para el análisis y el comercio.
- `Volume` – volumen de órdenes de mercado.
- `StopLossPoints` – distancia de stop loss en puntos.
- `TakeProfitPoints` – distancia de obtención de beneficios en puntos.

## Indicadores
- más alto
- Más bajo
