# Estrategia de ADX MACD Deev
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de ADX MACD Deev** es un port StockSharp del asesor experto de MetaTrader con el mismo nombre. Combina la señal de fuerza de tendencia del Índice Direccional Promedio (ADX) con la confirmación de impulso del Convergencia/Divergencia de Medias Móviles (MACD). La estrategia solo opera cuando ambos indicadores coinciden en la dirección del mercado y puede asegurar ganancias opcionalmente mediante stops de seguimiento y salidas parciales de posición.

## Cómo funciona
1. **Preparación de indicadores**
   - ADX se calcula con un período de promediado configurable. La estrategia rastrea los últimos valores de ADX y requiere que se muevan consistentemente en una dirección antes de permitir una operación.
   - MACD usa medias exponenciales móviles rápidas, lentas y de señal configurables. El histograma y la línea de señal deben mostrar conjuntamente un crecimiento sostenido para largos o una caída sostenida para cortos.
2. **Lógica de entrada**
   - **Entradas largas**: se activan cuando el histograma MACD supera el umbral `MACD Minimum (pips)`, tanto el histograma MACD como la línea de señal aumentan durante el número seleccionado de barras, y ADX se mantiene por encima de la fuerza requerida mientras también sube.
   - **Entradas cortas**: se activan cuando el histograma MACD está por debajo del umbral negativo, tanto el histograma MACD como la línea de señal declinan durante el intervalo seleccionado, y ADX permanece por encima del mínimo mientras decrece.
   - Solo puede haber una posición abierta a la vez.
3. **Gestión del riesgo**
   - Los niveles iniciales de stop-loss y take-profit se colocan en unidades de precio derivadas del instrumento `PriceStep` y las distancias de pip elegidas.
   - Un trailing stop puede seguir posiciones rentables una vez que el precio avanza `Trailing Stop + Trailing Step` pips.
   - Cuando `Take Half Profit` está habilitado, la estrategia cierra la mitad de la posición actual en el nivel de take-profit y deja el resto ejecutarse con el trailing stop.

## Parámetros
| Grupo | Nombre | Descripción |
| --- | --- | --- |
| Trading | Order Volume | Volumen de cada nueva orden de mercado. |
| Riesgo | Stop Loss (pips) | Desplazamiento inicial del stop-loss desde la entrada. |
| Riesgo | Take Profit (pips) | Desplazamiento inicial del take-profit desde la entrada. |
| Riesgo | Trailing Stop (pips) | Distancia del trailing stop. Establecer en cero para deshabilitar el trailing. |
| Riesgo | Trailing Step (pips) | Movimiento de precio adicional antes de que el trailing stop se mueva de nuevo. |
| Riesgo | Take Half Profit | Habilita la salida parcial cuando se alcanza el nivel de take-profit. |
| Indicadores | ADX Period | Período de promediado del ADX. |
| Indicadores | ADX Bars Interval | Número de barras ADX recientes que deben seguir una tendencia en una dirección. |
| Indicadores | ADX Minimum | Valor mínimo de ADX requerido para entradas. |
| Indicadores | MACD Fast EMA | Longitud de la EMA rápida utilizada por MACD. |
| Indicadores | MACD Slow EMA | Longitud de la EMA lenta utilizada por MACD. |
| Indicadores | MACD Signal EMA | Longitud de la EMA de señal utilizada por MACD. |
| Indicadores | MACD Bars Interval | Número de barras MACD que deben alinearse en la misma dirección. |
| Indicadores | MACD Minimum (pips) | Magnitud mínima del MACD convertida a pips. |
| General | Candle Type | Tipo de vela o marco temporal utilizado para los cálculos. |

## Notas de uso
- La estrategia requiere instrumentos con un `PriceStep` válido. Si `PriceStep` es cero, los umbrales basados en pips se revierten a valores crudos de MACD.
- El redondeo del volumen para salidas parciales sigue el `VolumeStep` del instrumento.
- Los ajustes del trailing stop se evalúan solo en velas cerradas.
- La estrategia usa enlaces de API de alto nivel (`SubscribeCandles().BindEx(...)`) y no depende del sondeo manual de valores de indicadores.
