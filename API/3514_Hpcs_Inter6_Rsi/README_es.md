# Estrategia Hpcs Inter6 RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Hpcs Inter6 RSI transfiere el MetaTrader experto `_HPCS_Inter6_MT4_EA_V01_WE` al API de alto nivel de StockSharp. El algoritmo observa el índice de fuerza relativa (RSI) en una serie de velas configurables y reacciona a reversiones rápidas alrededor de los umbrales clásicos 70/30. Siempre que RSI cruza por encima de 70, la estrategia pasa a una posición corta, mientras que un cruce por debajo de 30 pasa a una posición larga. Cada operación adjunta inmediatamente niveles simétricos de obtención de beneficios y limitación de pérdidas medidos en pips.

## Datos e indicadores
- **Fuente de vela**: período de tiempo seleccionado por el usuario (predeterminado 1 hora).
- **Indicador** – Índice de fuerza relativa con longitud configurable (predeterminado 14). El indicador se recalcula a través del proceso de vinculación del indicador StockSharp.

## Lógica de entrada
1. La estrategia espera a que se complete la vela para evitar operar con datos incompletos.
2. En cada vela completa, compara el nuevo valor RSI con el valor anterior.
3. **Configuración breve:** si RSI acaba de cruzar por encima de `UpperLevel` (70 predeterminado) desde abajo, la estrategia vende usando una orden de mercado. La exposición larga existente se cierra antes de que se establezca la venta corta, por lo que la posición neta resultante es corta exactamente en el volumen configurado.
4. **Configuración larga:** si RSI acaba de cruzar por debajo de `LowerLevel` (predeterminado 30) desde arriba, la estrategia compra usando una orden de mercado. Las posiciones cortas existentes se cubren primero, de modo que la posición neta se vuelve larga según el volumen configurado.
5. Sólo se permite una entrada por vela. Se ignoran varias señales dentro de la misma barra para reflejar la implementación MetaTrader que utiliza la protección de marca de tiempo de la barra.

## Lógica de salida
- Cada entrada define un objetivo fijo y se detiene a la misma distancia medida en pips.
- Mientras se está en una posición larga, la estrategia sale si el máximo de la vela toca el objetivo o si el mínimo toca el stop protector.
- Mientras se está en una posición corta, la estrategia cubre si el mínimo de la vela alcanza el objetivo o si el máximo alcanza el tope protector.
- Cuando la posición es plana, se borran todos los niveles de protección.

La distancia del pip se traduce en unidades de precio utilizando el tamaño del tick del instrumento. Para instrumentos con tres o cinco decimales, el algoritmo multiplica la distancia por diez para que coincida con la noción MetaTrader de un pip.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `CandleType` | plazo de 1 hora | Marco temporal que alimenta el indicador RSI. |
| `RsiLength` | 14 | Período retrospectivo del RSI. |
| `UpperLevel` | 70 | RSI nivel que activa entradas cortas cuando se cruza desde abajo. |
| `LowerLevel` | 30 | RSI nivel que activa entradas largas cuando se cruza desde arriba. |
| `TradeVolume` | 1 | Tamaño de la orden para entradas al mercado. La exposición existente se cierra antes de revertirse. |
| `OffsetInPips` | 10 | Distancia de la toma de ganancias y del stop-loss desde el precio de entrada, expresada en pips. |

Todos los parámetros están expuestos a través de objetos `StrategyParam` para que puedan optimizarse dentro de StockSharp.

## Notas
- La estrategia se basa en velas altas y bajas para simular tomas de ganancias y límites de pérdidas, coincidiendo con el comportamiento de los objetivos de precio fijo en MetaTrader.
- No se realizan pedidos pendientes; Todas las ejecuciones son órdenes de mercado manejadas por el núcleo de la estrategia.
- Los enlaces de indicador y gráfico se crean automáticamente cuando hay un área de gráfico disponible, lo que proporciona una superposición visual de velas y RSI.
