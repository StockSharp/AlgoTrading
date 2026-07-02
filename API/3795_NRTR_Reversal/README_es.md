# Estrategia de reversión de NRTR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
## Descripción general
La estrategia NRTR Reversal es una adaptación StockSharp del MetaTrader 4 experto "NRTR_Revers". El sistema original traza una línea de rango de seguimiento de reducción de ruido (NRTR) derivada del rango verdadero promedio (ATR) e invierte posiciones cada vez que el precio rompe de manera convincente esta barrera adaptativa. La versión StockSharp mantiene el comportamiento de posición única del asesor experto, refleja el cálculo de compensación basado en ATR y gestiona las salidas a través del módulo de protección integrado.

## Lógica comercial
1. Suscríbase a la serie de velas principal configurada por `CandleType` y procese solo velas terminadas, replicando la verificación del contador `Bars` de MetaTrader.
2. Alimente un indicador `AverageTrueRange` con el período `Period`. El valor ATR más reciente se traduce de unidades de precio a "puntos" (incrementos de precio) antes de multiplicarlo por `AtrMultiplier / 10`, al igual que la expresión MQL `MathRound(k * (iATR / Point) / 10)`.
3. Mantenga un caché continuo de velas recientes para reconstruir el pivote NRTR. El mínimo más bajo (para una tendencia alcista) o el máximo más alto (para una tendencia bajista) sobre las últimas `Period` velas se convierte en el pivote base.
4. Cambie el pivote según el desplazamiento basado en ATR para formar la línea final:
   - Tendencia alcista: `line = lowestLow - offset`.
   - Tendencia bajista: `line = highestHigh + offset`.
5. Detectar una reversión siempre que se cumpla cualquiera de las condiciones:
   - **Ruptura de cierre:** el último cierre de vela cruza la línea en más de `offset` puntos.
   - **Expansión del rango:** las velas `Period / 2` más recientes se extienden más allá de la línea en al menos `ReverseDistancePoints` puntos. Esto reproduce la prueba de reversión secundaria del código MQL que se remonta a más atrás en la historia.
6. Cuando cambie la dirección, envíe una orden de mercado (`BuyMarket` o `SellMarket`) con volumen `TradeVolume + |Position|`. Esto cierra la exposición opuesta y abre la nueva posición, coincidiendo con el comportamiento MetaTrader de cerrar y revertir inmediatamente.
7. Las salidas se delegan al administrador de riesgos iniciado por `StartProtection`, que convierte las distancias configuradas de stop-loss y take-profit desde puntos en unidades de precio específicas del corredor.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | plazo de 15 minutos | Serie de velas utilizadas para los cálculos. |
| `TakeProfitPoints` | `decimal` | `4000` | Distancia de obtención de beneficios expresada en incrementos del precio del instrumento. Establezca en cero para desactivar. |
| `StopLossPoints` | `decimal` | `4000` | Distancia de stop-loss en pasos de precio. Establezca en cero para desactivar. |
| `TrailingStopPoints` | `decimal` | `0` | Parámetro reservado para módulos finales externos. No se utiliza dentro de la estrategia. |
| `TradeVolume` | `decimal` | `0.1` | Volumen base (lotes) reflejado desde la configuración MetaTrader. |
| `Period` | `int` | `3` | Número de velas utilizadas para calcular el pivote NRTR. |
| `ReverseDistancePoints` | `int` | `100` | Distancia de ruptura adicional en puntos requeridos para la confirmación. |
| `AtrMultiplier` | `decimal` | `3.0` | Multiplicador aplicado a ATR antes de generar la compensación. |

## Gestión de riesgos
- La estrategia llama a `StartProtection` con `UnitTypes.Step`, por lo que las distancias de puntos configuradas se convierten automáticamente en compensaciones de precios absolutas basadas en `Security.PriceStep`.
- Si tanto el stop-loss como la toma de ganancias son cero, todavía se llama a `StartProtection()` para habilitar el monitoreo de posición de StockSharp, replicando los controles de seguridad utilizados por EA.
- `TrailingStopPoints` se expone para que esté completo, pero se deja para futuras extensiones, porque el experto original no implementó una función de seguimiento a pesar de declarar el parámetro.

## Detalles de implementación
- La estrategia se basa exclusivamente en el API (`SubscribeCandles().BindEx(...)`) de alto nivel con enlaces de indicadores; no se utilizan bucles de indicadores manuales ni llamadas `GetValue` prohibidas.
- Una estructura compacta `CandleSnapshot` mantiene solo los valores alto/bajo/cierre de velas recientes, evitando un almacenamiento pesado `ICandleMessage` y al mismo tiempo reproduce las ventanas retrospectivas de NRTR.
- La conversión de ATR a puntos respeta la fórmula MetaTrader dividiendo el ATR por el paso del instrumento antes de aplicar el multiplicador y el redondeo.
- El recorte del historial mantiene el caché en `Period * 3` velas para que coincida con las necesidades retrospectivas originales sin un crecimiento descontrolado.

## Diferencias con el experto MetaTrader
- El cierre de órdenes se simplifica: en lugar de iterar a través de cada operación y llamar a `OrderClose`, el puerto StockSharp envía una única orden de mercado que favorece la posición existente y establece la nueva dirección.
- Los números mágicos, el deslizamiento y los parámetros específicos del ticket se omiten porque StockSharp gestiona los pedidos de forma diferente.
- Las anotaciones en los gráficos son opcionales; cuando hay un área de gráfico disponible, la serie ATR y las operaciones propias se trazan con fines de depuración.

## Consejos de uso
- Alinee `TradeVolume` con el paso del lote de intercambio (`Security.VolumeStep`) antes de habilitar las operaciones en vivo.
- Sintonice `Period`, `AtrMultiplier` y `ReverseDistancePoints` juntos. Los períodos más cortos requieren distancias inversas más pequeñas para evitar el exceso de operaciones.
- Establezca distancias de parada/objetivo según el tamaño del tick del instrumento. En instrumentos con `PriceStep` grande, reduzca las compensaciones predeterminadas de 4000 puntos a niveles realistas.

## Indicadores
- `AverageTrueRange(Period)` calculado sobre precios máximo/bajo/cierre.
