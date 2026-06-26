# Estrategia Exp de Ajuste Fino de Velas MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Convertida del experto de MetaTrader 5 `Exp_FineTuningMACandle.mq5`, que opera según el color del indicador *Fine Tuning MA Candle*.
- Diseñada para la API de alto nivel de StockSharp: se suscribe a una serie de velas, obtiene los valores del indicador mediante `BindEx` y enruta todas las órdenes a través de los métodos auxiliares de `Strategy`.
- Implementa los mismos permisos de entrada y cierres condicionales que el experto original, respetando el modelo de ejecución asíncrona de StockSharp.

## Indicador Fine Tuning MA Candle
- El indicador construye velas OHLC sintéticas aplicando un esquema de ponderación en tres etapas a las últimas `Length` velas de la serie de precios.
  - `Rank1`, `Rank2` y `Rank3` controlan la curvatura de las rampas de ponderación, mientras que `Shift1`, `Shift2` y `Shift3` combinan las rampas con un componente plano.
  - La ponderación es simétrica: la primera mitad de la ventana se acelera hacia el centro, la segunda mitad se desacelera alejándose de él.
  - Tras la normalización, las cuatro sumas ponderadas producen precios suavizados de apertura, máximo, mínimo y cierre.
- Cuando la apertura y el cierre suavizados difieren en menos de `GapPoints` (convertido al paso de precio del instrumento), la apertura se reemplaza por el cierre sintético anterior para eliminar los huecos de precio.
- La vela se colorea **2** (alcista) cuando `Open < Close`, **0** (bajista) cuando `Open > Close`, y **1** cuando son iguales. Solo se utiliza el flujo de color para las decisiones de trading.
- `PriceShiftPoints` desplaza verticalmente cada vela sintética un número configurable de pasos de precio.

## Reglas de trading
- Las señales se generan únicamente en velas completadas. La estrategia almacena los colores del indicador y evalúa la vela ubicada `SignalBar` pasos detrás de la última finalizada.
- **Rotación alcista (el color cambia a 2):**
  - Las posiciones cortas existentes se cierran si `SellPosClose` está activado.
  - Una vez que la posición es plana, y si `BuyPosOpen` está permitido, se envía una orden de mercado larga por `Volume` lotes. Si antes hubo que cerrar un corto, la entrada larga se encola y se ejecuta en cuanto la posición vuelve a cero.
- **Rotación bajista (el color cambia a 0):**
  - Las posiciones largas existentes se cierran si `BuyPosClose` está activado.
  - Una vez plana, y si `SellPosOpen` está permitido, se envía una orden de mercado corta por `Volume` lotes. Las entradas pendientes se gestionan igual que para las señales largas.
- El color neutral (1) no desencadena ninguna acción.
- Las órdenes nunca se apilan: la estrategia mantiene como máximo una posición a la vez y espera a que las posiciones activas se cierren antes de revertir.

## Gestión del riesgo
- `StopLossPoints` y `TakeProfitPoints` representan distancias en pasos de precio. Tras llenar una nueva posición, la estrategia registra órdenes de protección y objetivo usando el precio de llenado real reportado en `OnNewMyTrade`.
- Las órdenes de protección se cancelan automáticamente cuando la posición vuelve a cero o cuando se encola una nueva orden.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `CandleType` | Tipo de datos/período de las velas usadas para los cálculos del indicador. |
| `Length` | Número de velas procesadas por la ventana ponderada del indicador. |
| `Rank1`, `Rank2`, `Rank3` | Coeficientes de potencia que dan forma a las tres etapas de ponderación. |
| `Shift1`, `Shift2`, `Shift3` | Factores de mezcla (0–1) que combinan las etapas de ponderación con un componente plano. |
| `GapPoints` | Diferencia máxima entre apertura y cierre sintéticos que se suprime copiando el cierre anterior. Expresada en pasos de precio. |
| `SignalBar` | Cuántas velas cerradas omitir antes de leer el color del indicador. `1` significa "usar la última vela completada". |
| `BuyPosOpen` / `SellPosOpen` | Permitir abrir posiciones largas/cortas. |
| `BuyPosClose` / `SellPosClose` | Permitir cerrar posiciones largas/cortas cuando aparece el color opuesto. |
| `StopLossPoints` | Distancia desde el precio de llenado al stop de protección. Establezca en `0` para desactivar. |
| `TakeProfitPoints` | Distancia desde el precio de llenado al objetivo de ganancia. Establezca en `0` para desactivar. |
| `PriceShiftPoints` | Desplazamiento vertical aplicado a las velas sintéticas, expresado en pasos de precio. |

## Notas de implementación
- Utiliza `BindEx` porque el indicador personalizado devuelve un objeto de valor complejo que expone el OHLC sintético y el color simultáneamente.
- Mantiene solo un pequeño historial de valores de color (`SignalBar + 2` entradas) para detectar cambios de color sin almacenar grandes búferes.
- Los reversals de entrada respetan el modelo de ejecución asíncrona esperando a que la posición se aplane antes de enviar la orden del lado opuesto.
