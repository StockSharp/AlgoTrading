# Estrategia del Sistema de Canal de Cruce Porcentual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un port directo del asesor experto de MetaTrader *Exp_PercentageCrossoverChannel_System*. Rastrea cómo el precio interactúa con un "Percentage Crossover Channel" personalizado y reacciona cuando los candles vuelven al interior del canal después de haber roto previamente. El código fue reescrito con las APIs de alto nivel de StockSharp y preserva el flujo de señales original.

## Lógica de trading

1. **Construcción del indicador**
   - El Percentage Crossover Channel construye una línea media adaptativa que permanece cerca del precio pero no puede alejarse más rápido que un porcentaje fijo (`Percent`).
   - Las bandas superior e inferior se derivan de la línea media usando la misma distancia porcentual.
   - Cada candle completado recibe color según su relación con el canal de hace `Shift` barras:
     - Color `3` / `4`: cierre por encima de la banda superior (cuerpo de candle bajista/alcista respectivamente).
     - Color `0` / `1`: cierre por debajo de la banda inferior (cuerpo bajista/alcista respectivamente).
     - Color `2`: el candle terminó dentro del canal.

2. **Reglas de entrada y salida**
   - Evaluar el último candle de `SignalBar` y el inmediatamente anterior (espeja la llamada `CopyBuffer` de MQL).
   - **Secuencia alcista** (`olderColor > 2`): el mercado recientemente cerró por encima del canal. Si el candle más reciente volvió al interior (`recentColor < 3`) la estrategia:
     - Cierra cualquier corto activo si `SellPositionsClose` está habilitado.
     - Abre una posición larga cuando no hay operaciones abiertas y `BuyPositionsOpen` está habilitado.
   - **Secuencia bajista** (`olderColor < 2`): el mercado recientemente cerró por debajo del canal. Si el último candle retornó al interior (`recentColor > 1`) la estrategia:
     - Cierra cualquier largo si `BuyPositionsClose` está habilitado.
     - Abre una posición corta cuando no hay operaciones activas y `SellPositionsOpen` está habilitado.
   - La lógica por tanto espera una ruptura seguida de una re-entrada al canal antes de comprometerse en la dirección de la ruptura.

3. **Gestión del riesgo**
   - El stop loss y take profit opcionales se expresan en pasos de precio y se evalúan en los máximos/mínimos de los candles.
   - Si se activa una orden de protección la estrategia sale del mercado e ignora nuevas entradas para la misma barra, imitando el comportamiento de MQL donde los stops del broker cierran la operación primero.

## Parámetros

| Nombre | Descripción |
| ---- | ----------- |
| `Percent` | Ancho del canal en porcentaje. Coincide con el parámetro de entrada del indicador MQL. |
| `Shift` | Número de barras usadas para comparar la ruptura con las bandas históricas. |
| `SignalBar` | Desplazamiento (en barras) para la evaluación de señales. Un valor de 1 significa "barra anterior" como el valor predeterminado del EA original. |
| `BuyPositionsOpen` / `SellPositionsOpen` | Habilitar o deshabilitar la apertura de operaciones en la dirección correspondiente. |
| `BuyPositionsClose` / `SellPositionsClose` | Habilitar o deshabilitar el cierre forzado de posiciones opuestas ante una nueva señal. |
| `StopLoss` | Distancia del stop loss expresada en múltiplos de `Security.PriceStep`. Poner en cero para deshabilitar. |
| `TakeProfit` | Distancia del take-profit en pasos de precio. Poner en cero para deshabilitar. |
| `CandleType` | Marco temporal para la suscripción de candles. Por defecto barras de cuatro horas para reflejar `PERIOD_H4`. |

## Notas de implementación

- La lógica del indicador está implementada inline porque StockSharp no proporciona un Percentage Crossover Channel nativo. Los cálculos de la línea media, derivación de bandas y asignaciones de color reproducen el algoritmo fuente de MQL paso a paso.
- La gestión de posiciones sigue las funciones auxiliares originales (`BuyPositionOpen`, `SellPositionOpen`, etc.) cerrando operaciones opuestas antes de abrir una nueva y omitiendo entradas cuando hay una posición opuesta activa.
- La gestión del dinero, el manejo de desviaciones y el dimensionamiento de lotes específico del modo de margen del archivo include original no están replicados. Los usuarios de StockSharp deben configurar el volumen de la estrategia a través de las propiedades estándar de `Strategy` o del entorno de hospedaje.
- Los valores de stop loss / take profit se interpretan como *pasos de precio* porque los parámetros de MetaTrader se especifican en puntos. Asegúrese de que el instrumento conectado exponga un `PriceStep` válido.

## Consejos de uso

- Conecte la estrategia a un instrumento con datos confiables de cuatro horas si desea un comportamiento idéntico a MetaTrader. Ajuste `CandleType` para experimentar con operación intradía.
- Dado que la lógica de entrada requiere dos candles completados con información de color válida, permita que la estrategia se caliente con al menos `Shift + SignalBar + 1` barras de historial.
- El canal es sensible al parámetro `Percent`. Valores más pequeños se ajustan estrechamente al precio y aumentan la frecuencia de trading, mientras que valores más grandes se enfocan en rupturas más fuertes.
- Al combinar con controles de riesgo a nivel de cartera, tenga en cuenta que esta implementación abre como máximo una posición a la vez y alterna entre estados largo, plano o corto.
