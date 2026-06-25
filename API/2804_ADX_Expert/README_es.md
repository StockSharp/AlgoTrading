# Estrategia ADX Expert
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia ADX Expert** es una conversión directa del expert advisor original de MetaTrader 4 "ADX Expert" (script MQL 20315). El expert busca cruces entre las líneas del Directional Index positivo y negativo (+DI y -DI) mientras el Average Directional Index (ADX) permanece por debajo de un umbral especificado, indicando que el mercado está en rango. Solo puede haber una posición abierta a la vez, igual que en el expert fuente.

## Lógica de trading
1. La estrategia se suscribe a la serie de velas seleccionada (velas de 15 minutos por defecto) y calcula el Average Directional Index con el período configurado.
2. Se coloca una orden de compra cuando:
   - La línea +DI cruza por encima de la línea -DI.
   - El valor del ADX se mantiene por debajo del umbral definido (por defecto 20), señalando una tendencia débil.
   - El spread actual está por debajo del filtro `MaxSpreadPoints`.
   - No hay ninguna posición abierta actualmente.
3. Se coloca una orden de venta cuando:
   - La línea +DI cruza por debajo de la línea -DI.
   - El valor del ADX sigue siendo menor que el umbral permitido.
   - Se satisfacen el requisito de spread y la condición de posición plana.
4. Los niveles de stop-loss y take-profit protectores se asignan a través de `StartProtection`, replicando el stop y objetivo fijos de la versión MQL. Se expresan en puntos de precio (pasos de precio) y se pueden desactivar estableciendo los valores en cero.

La estrategia se basa en un flujo de trabajo de posición única: las nuevas señales se ignoran hasta que la posición actual sea cerrada por sus órdenes protectoras.

## Parámetros
| Parámetro | Descripción | Por defecto |
| --- | --- | --- |
| `TradeVolume` | Tamaño de orden utilizado para cada orden de mercado. | 0.1 |
| `AdxPeriod` | Período para el cálculo del ADX. | 14 |
| `AdxThreshold` | Valor máximo del ADX que aún permite una operación. | 20 |
| `MaxSpreadPoints` | Spread máximo permitido en puntos de precio. Establecer en 0 para deshabilitar el filtro. | 20 |
| `StopLossPoints` | Distancia de stop-loss en puntos de precio. | 200 |
| `TakeProfitPoints` | Distancia de take-profit en puntos de precio. | 400 |
| `CandleType` | Tipo de vela para los cálculos de indicadores (velas de 15 minutos por defecto). | Marco temporal de 15 minutos |

## Notas adicionales
- El filtro de spread requiere actualizaciones del libro de órdenes para leer los mejores precios de bid y ask. Asegúrese de que su proveedor de datos suministre esta información.
- Todos los comentarios y registros están escritos en inglés para mayor claridad, cumpliendo con las pautas del repositorio.
- La estrategia está destinada a fines educativos. Pruébela exhaustivamente en un entorno simulado antes de implementarla en trading en vivo.
