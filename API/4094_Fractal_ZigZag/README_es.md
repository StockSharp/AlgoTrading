# Estrategia fractal en zigzag
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una adaptación directa del asesor experto MetaTrader 4 **Fractal ZigZag Expert.mq4**. Reconstruye el Proyecto de Ley Williams
secuencia fractal e interpreta el extremo confirmado más reciente como el tramo activo del mercado. Cuando el último fractal válido es un
al girar hacia abajo, el sistema abre una posición larga; cuando se confirma un máximo, se abre un corto. La implementación mantiene el
parámetros originales (profundidad fractal, toma de ganancias, distancias de stop inicial y trailing stop) mientras se adapta la ruta de la orden a
el nivel alto StockSharp API.

La estrategia es más adecuada para velas H1, ya que replica el gráfico predeterminado utilizado en la versión MetaTrader. Sin embargo, el
El parámetro `CandleType` permite cambiar a cualquier otro período admitido por la fuente de datos. Todas las distancias están expresadas en precio.
puntos (pasos del precio del instrumento), que refleja la forma en que MetaTrader usa la constante `Point`.

## Reglas comerciales

- **Detección de señal**
  - El algoritmo escanea cada vela terminada y crea una ventana móvil con `2 * Level + 1` elementos.
  - Un fractal alto se confirma cuando la vela del medio tiene el máximo más alto dentro de esa ventana; un fractal bajo requiere el más bajo
bajo.
  - Sólo el último fractal confirmado controla la dirección: un mínimo establece la tendencia interna en `2` (alcista), un máximo la establece en
`1` (bajista).
- **Inscripciones**
  - Cuando la tendencia interna es igual a `2` y no hay ninguna posición abierta, se envía una compra de mercado utilizando el volumen `Lots`.
  - Cuando la tendencia es igual a `1` sin posición, se envía una venta de mercado.
  - La estrategia volverá a entrar en la misma dirección después de que se cierre una posición si la tendencia no ha cambiado.
- **Salidas y gestión de riesgos**
  - Cada entrada recibe un stop loss inicial y un takeprofit fijo definido en puntos. Un valor de parada de `0` desactiva el
protección respectiva.
  - El trailing stop opcional (también en puntos) se activa una vez que el precio se mueve la distancia configurada. Luego la parada se traslada a
mantener la misma compensación desde el precio de cierre, sin cruzar nunca el stop de protección inicial.
  - Las órdenes de protección se emulan monitoreando los máximos y mínimos de las velas para aproximarse a los toques intrabar, coincidiendo estrechamente con el original.
MQL4 lógica.

## Parámetros predeterminados

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `Level` | `2` | Número de velas de cada lado necesarias para confirmar un fractal. |
| `TakeProfitPoints` | `25` | Distancia al objetivo de toma de ganancias en puntos de precio. |
| `InitialStopPoints` | `20` | Distancia al stop loss inicial en puntos de precio. |
| `TrailingStopPoints` | `10` | Distancia del trailing stop en puntos de precio (establecido en `0` para desactivarlo). |
| `Lots` | `1` | Volumen de órdenes utilizado para las entradas al mercado. |
| `CandleType` | `H1` | Plazo de velas procesadas por la estrategia. |

## Notas

- La estrategia llama a `StartProtection()` una vez al inicio para que StockSharp pueda gestionar la liquidación de posiciones de emergencia si es necesario.
- Todos los registros y comentarios se proporcionan en inglés, mientras que las descripciones siguen el idioma de cada variante README según lo exige la
pautas de conversión.
- La implementación evita los buffers de indicadores e imita el enfoque MetaTrader manteniendo solo la ventana móvil mínima.
necesario para evaluar un fractal.
