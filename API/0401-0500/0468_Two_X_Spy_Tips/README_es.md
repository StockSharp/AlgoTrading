# Two X Spy Tips

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Reglas de negociación

SpySecurity y TipsSecurity son señales explícitas distintas de Universe; Security también pertenece al conjunto. En la primera vela cerrada sincronizada mensual, ambos cierres diarios completos deben superar estrictamente su SMA de SmaLength días, incluido el cierre actual. Un filtro formado fallido consume la revisión del mes; falta de historial la retrasa. Desde cero comprar Capital×Leverage/precio redondeado a VolumeStep. Mantener largo sin salidas, cortos ni acumulaciones mensuales adicionales. Usar USD y respetar MinTradeUsd y límites nativos.

## Parámetros

| Parámetro | Predeterminado |
|---|---|
| `Universe` | obligatorio |
| `Capital` | 100000 |
| `Leverage` | 2 |
| `MinTradeUsd` | 200 |
| `CandleType` | TimeSpan.FromMinutes(1440).TimeFrame() |
| `PriceHistoryFile` | sin configurar |
| `SpySecurity` | obligatorio |
| `TipsSecurity` | obligatorio |
| `SmaLength` | 200 |

## Datos de entrada

CSV opcional `PriceHistoryFile`: `SecurityId,Date,Close`, precio positivo, pareja título/fecha única, fecha `yyyy-MM-dd`. Intradía solo fechas UTC anteriores; una vela diaria cerrada aporta su cierre. Filas futuras no anticipan decisiones. Las velas sustituyen su valor diario.

## Verificación

Ambos lenguajes se verifican con BTC/TON incluido, fórmulas independientes, dirección/tamaño de órdenes, ejecuciones, posiciones y salidas. Datos fechados de precios/factores y calendarios verifican mecánica; OHLC/volúmenes trasladados no son historia plurianual. Dos grupos, TopN=1 y SMA=2 son perfiles explícitos pequeños. Ventanas largas y valores originales se comprueban aparte. Sin promesas de rentabilidad.
