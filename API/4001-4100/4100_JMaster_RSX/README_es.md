# Estrategia JMaster RSX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia JMaster RSX es una conversión directa del MetaTrader 4 asesor experto **jMasterRSXv1**. El sistema alinea los valores del oscilador Jurik RSX calculados en un período de tiempo rápido (M5) y lento (M30). Cuando el marco temporal más alto apunta en una dirección alcista o bajista y el oscilador rápido alcanza territorio de sobreventa/sobrecompra, la estrategia entra en una posición en la dirección correspondiente. Todas las señales se evalúan en la apertura de la nueva barra utilizando las velas completamente cerradas anteriores, coincidiendo con la implementación MT4 que hacía referencia a los valores `shift = 1`.

## Indicadores y datos
- **Jurik RSX (Longitud = `RsxLength`) en el período de tiempo rápido**: evalúa el oscilador en la serie de velas definida por `FastCandleType` (barras predeterminadas de 5 minutos). La conversión reproduce el filtro recursivo original utilizado por el indicador personalizado `rsx.mq4`.
- **Jurik RSX en el marco de tiempo lento**: calculado con la misma longitud en la serie de velas definida por `SlowCandleType` (barras predeterminadas de 30 minutos). El último valor lento completado se retrasa una barra antes de usarse, reflejando el comportamiento de cambio de MT4.

## Lógica de entrada
1. Espere a que se abra una nueva vela rápida (equivalente a procesar una vela terminada en StockSharp).
2. Recupere la lectura RSX rápida anterior y la lectura RSX lenta anterior (una vela lenta detrás del cierre actual).
3. **Configuración larga:** el RSX lento está por encima del `MidlineLevel` (predeterminado 50) *y* el RSX rápido está por debajo del `OversoldLevel` (predeterminado 25).
4. **Configuración breve:** el RSX lento está por debajo de `MidlineLevel` *y* el RSX rápido está por encima de `OverboughtLevel` (predeterminado 75).
5. Abra una orden de mercado con volumen `Volume` cuando no haya ninguna posición activa actualmente.

## Salir de la lógica
- Cierre una posición larga abierta tan pronto como se cumplan las condiciones cortas (RSX lento por debajo de la línea media y RSX rápido por encima del umbral de sobrecompra).
- Cierre una posición corta abierta tan pronto como se cumplan las condiciones largas (RSX lento por encima de la línea media y RSX rápido por debajo del umbral de sobreventa).
- La estrategia no acumula posiciones; siempre se reduce a un estado plano antes de considerar una nueva entrada.

## Dimensionamiento de posiciones
- Los pedidos se realizan con un volumen fijo controlado por el parámetro `Volume` (por defecto `0.1`).
- No se implementa ninguna lógica piramidal o de gestión adaptativa del dinero. Esto refleja el comportamiento predeterminado del EA original cuando `DecreaseFactor` se dejó en cero.

## Parámetros
| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `FastCandleType` | Tipo de vela para el cálculo rápido de RSX | `M5` |
| `SlowCandleType` | Tipo de vela para el cálculo lento de RSX | `M30` |
| `RsxLength` | Longitud retrospectiva compartida por ambas instancias RSX | `14` |
| `OverboughtLevel` | Umbral RSX rápido para entradas cortas | `75` |
| `OversoldLevel` | Umbral RSX rápido para entradas largas | `25` |
| `MidlineLevel` | Lenta línea media del RSX que separa los regímenes alcista/bajista | `50` |
| `Volume` | Volumen de pedidos para entradas al mercado | `0.1` |

## Notas de uso
- Asegúrese de que los datos históricos entreguen velas terminadas para ambos períodos de tiempo configurados; la estrategia sólo reacciona después del cierre de una vela.
- Debido a que el valor RSX lento se retrasa deliberadamente una barra, las reversiones intrabarra en el período de tiempo más alto aparecerán una barra más tarde; esto coincide con la fuente EA y evita el sesgo de anticipación.
- El indicador RSX integrado genera valores en el rango de 0 a 100, lo que permite una comparación directa con otros osciladores si lo desea.
