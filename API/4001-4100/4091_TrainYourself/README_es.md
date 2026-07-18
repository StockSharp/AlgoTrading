# Estrategia entrénate
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una versión StockSharp del asesor experto MetaTrader 4 **TrainYourself-V1_1-1**. Recrea la lógica de creación de canales y de ruptura al tiempo que reemplaza los botones gráficos del script MT4 con llamadas explícitas a métodos. El algoritmo reconstruye continuamente un canal de precios estilo Donchian y activa una operación una vez que el precio sale del canal después de consolidarse primero dentro de él.

## Lógica comercial

1. **Construcción del canal**
   - Un indicador `DonchianChannels` con `ChannelLength` períodos se evalúa en cada vela terminada del `CandleType` seleccionado.
   - Las bandas superior e inferior sin procesar se expanden con un búfer similar a MetaTrader adicional: `BufferPoints` multiplicado por el instrumento `PriceStep`. Esto reproduce el guión original que inicialmente colocó las líneas de tendencia a 50 puntos de la oferta/demanda actual antes de deslizarlas sobre máximos y mínimos recientes.
   - Los valores `UpperBand`/`LowerBand` resultantes se exponen como propiedades de solo lectura para que puedan mostrarse en paneles personalizados.

2. **Condición de armado**
   - El motor de ruptura permanece desarmado mientras una posición está abierta o cuando `EnableTrendTrade` es falso.
   - Cuando no hay posición, el precio debe cerrar dentro del canal con un margen adicional de `ActivationPoints` * `PriceStep` desde ambos límites. Solo entonces `_isArmed` se convierte en `true`, imitando la bandera MetaTrader `q=1` que se estableció cuando el precio volvió al canal.

3. **Ejecución de ruptura**
   - Una vez armado, un cierre en o por encima de `UpperBand` coloca una orden de compra de mercado (si `AllowBuyOpen` está habilitado). Un cierre en o por debajo del `LowerBand` coloca una orden de venta de mercado (con respecto a `AllowSellOpen`).
   - Después de realizar una orden, la estrategia se desarma hasta que el precio vuelve a ingresar al canal sin ninguna posición abierta.

4. **Gestión de riesgos**
   - `StartProtection` configura órdenes de protección automáticas. Las distancias se calculan multiplicando `TakeProfitPoints` y `StopLossPoints` por el `PriceStep` actual. Si el corredor no informa un paso, se utiliza un respaldo de `0.0001`, que coincide con el comportamiento de MetaTrader `Point`.

5. **Controles manuales**
   - Las etiquetas MT4 (`BUY_TRIANGLE`, `SELL_TRIANGLE`, `CLOSE_ORDER`) se reemplazan por tres métodos públicos: `TriggerManualBuy()`, `TriggerManualSell()` y `ClosePositionManually()`. Respetan `AllowBuyOpen`/`AllowSellOpen`, verifican el estado de la conexión a través de `IsFormedAndOnlineAndAllowTrading()` y también desarman la lógica de ruptura para que las operaciones manuales no activen inmediatamente entradas automáticas.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `CandleType` | `30m` período de tiempo | Suscripción de vela principal utilizada para todos los cálculos. |
| `ChannelLength` | `20` | Número de velas analizadas por el canal Donchian. |
| `BufferPoints` | `50` | MetaTrader puntos adicionales agregados alrededor del último cierre antes de finalizar el canal. |
| `ActivationPoints` | `2` | Margen (en puntos) que el precio debe mantener alejado de los bordes del canal antes de que se pueda armar una ruptura. |
| `StopLossPoints` | `100` | Distancia de stop-loss en puntos; convertido a precio absoluto multiplicando por `PriceStep`. |
| `TakeProfitPoints` | `100` | Distancia de toma de ganancias en puntos; convertido a precio absoluto usando `PriceStep`. |
| `EnableTrendTrade` | `true` | Permite el comercio de ruptura automática. Cuando `false` solo los métodos auxiliares manuales pueden abrir/cerrar posiciones. |
| `Volume` | `1` | Tamaño del pedido para operaciones automáticas y manuales. |

## Notas de uso

- El asesor experto original requería arrastrar íconos en el gráfico para (re)construir líneas de tendencia. En StockSharp el canal se reconstruye automáticamente en cada vela, por lo que no es necesaria una actualización manual.
- Debido a que la estrategia expone `UpperBand`, `LowerBand` y `IsArmed`, los paneles o widgets de interfaz de usuario pueden replicar la información visual original sin depender de los objetos del gráfico.
- Los niveles de stop-loss y take-profit son opcionales. Establezca los parámetros correspondientes en `0` para deshabilitar las órdenes de protección, reflejando el comportamiento de MetaTrader donde las rutinas de modificación se omitieron cuando el valor externo era cero.
- Las entradas manuales respetan el mismo parámetro `Volume` y se benefician automáticamente de las distancias de protección configuradas.
- Para restablecer el estado de ruptura manualmente, llame a `ClosePositionManually()` (que también borra `IsArmed`) o espere a que el precio vuelva a ingresar al canal para que se cumpla nuevamente la condición de armado.
