# Estrategia de Promediado de Canal Firebird
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General
La estrategia de Promediado de Canal Firebird replica el experto de MetaTrader 5 "Firebird v0.60" usando la API de alto nivel de StockSharp. Opera en un canal de media móvil configurable y promedia progresivamente en posiciones cuando el precio se aleja del canal. El enfoque está diseñado para el trading de forex de reversión a la media donde se requieren entradas de estilo grid y controles de riesgo basados en pips.

## Configuración de Indicadores
- Se calcula una media móvil (simple, exponencial, suavizada o ponderada) sobre la serie de velas seleccionada. La fuente de precio (cierre, máximo, mínimo, mediana, etc.) puede configurarse.
- Las bandas superiores e inferiores del canal se derivan desplazando la media móvil por un porcentaje definido por el usuario.

## Lógica de Entrada
1. **Condiciones de Compra**
   - El precio de la fuente de vela elegida cierra por debajo de la banda inferior.
   - No existe posición, o la nueva entrada está al menos `Step (pips)` lejos del último relleno al tener en cuenta el crecimiento de `Step Exponent`.
   - La estrategia aplica un período de espera de dos intervalos de vela entre entradas.
2. **Condiciones de Venta**
   - El precio cierra por encima de la banda superior.
   - Las verificaciones de distancia y enfriamiento idénticas a la lógica larga deben satisfacerse.

Cuando ocurre una señal válida, la estrategia envía una orden de mercado con el volumen de lotes configurado. Solo se mantiene una dirección a la vez: las señales opuestas esperarán hasta que el inventario actual sea cerrado por las reglas de riesgo.

## Gestión de Posiciones
- Cada entrada se almacena para que la estrategia pueda calcular el precio promedio del grid abierto.
- Los niveles de stop loss y take profit se definen en pips. Para una posición única, el stop loss equivale al precio de entrada menos/más `Stop Loss (pips)` y el take profit equivale al precio de entrada más/menos `Take Profit (pips)`.
- Cuando existen múltiples posiciones, la distancia del stop loss se divide por el número de entradas, emulando el comportamiento de promediado del experto original.
- Los objetivos de beneficio permanecen fijos relativos al precio promedio, mientras que las salidas de stop loss se recalculan en cada vela.
- El trading puede desactivarse opcionalmente los viernes.

## Parámetros
| Parámetro | Descripción |
| --- | --- |
| `Volume` | Tamaño de orden en lotes para cada entrada promediada (por defecto 0.1). |
| `Stop Loss (pips)` | Distancia del stop protector en pips (por defecto 50). |
| `Take Profit (pips)` | Distancia del take profit en pips (por defecto 150). |
| `MA Period` | Longitud de lookback de la media móvil (por defecto 10). |
| `MA Shift` | Desplazamiento adelantado en velas aplicado a la salida de la media móvil. |
| `MA Type` | Método de cálculo de la media móvil: Simple, Exponencial, Suavizada o Ponderada. |
| `Price Source` | Precio de vela usado para los cálculos del indicador (por defecto cierre). |
| `Channel %` | Desplazamiento porcentual desde la media móvil usado para formar las bandas (por defecto 0.3%). |
| `Trade Friday` | Habilita o deshabilita el trading los viernes. |
| `Step (pips)` | Distancia mínima en pips entre órdenes promediadas (por defecto 30). |
| `Step Exponent` | Exponente que escala el paso según el número de entradas abiertas (0 mantiene el paso constante). |
| `Candle Type` | Marco temporal para las velas de trabajo. |

## Notas
- La estrategia asume que el `PriceStep` del instrumento representa un pip. Si no está disponible, recurre a 0.0001.
- Las salidas protectoras se ejecutan con órdenes de mercado en lugar de órdenes nativas de stop/limit para mantenerse consistente con la API de alto nivel.
- El grid de promediado está limitado por la lógica de enfriamiento y por la distancia creciente cuando se usa un exponente de paso mayor que cero.
