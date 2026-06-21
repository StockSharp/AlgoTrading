# Estrategia Roboti ADX Profit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia convierte el asesor experto original **RobotiADXProfitwining.mq4** a la API de StockSharp. Se basa en el Índice de Movimiento Direccional (DMI) para determinar la dirección de la tendencia.

## Lógica de trading

- Utiliza el indicador `DirectionalIndex` con un período predeterminado de 14.
- Trabaja con velas de una hora por defecto, pero el marco temporal puede cambiarse.
- Abre una posición **larga** cuando la línea `+DI` cruza por encima de la línea `-DI` y no hay ninguna posición larga abierta.
- Abre una posición **corta** cuando la línea `-DI` cruza por encima de la línea `+DI` y no hay ninguna posición corta abierta.
- Las posiciones están protegidas por un trailing stop expresado como porcentaje del precio.

## Parámetros

| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `DmiPeriod` | Período para el cálculo del DMI. | 14 |
| `CandleType` | Tipo de vela y marco temporal utilizado por la estrategia. | 1 hora |
| `TrailingStopPercent` | Tamaño del trailing stop en porcentaje. | 1% |

## Notas

La estrategia utiliza la API de vinculación de alto nivel de StockSharp y evita llamadas directas a los buffers de indicadores. Todos los comentarios en el código están en inglés.
