# Estrategia de proyección Gandalf PRO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Gandalf PRO es una versión StockSharp del MetaTrader 4 asesor experto *Gandalf_PRO*. El robot original construye un
filtro de suavizado adaptativo a partir de una media móvil ponderada y un componente de tendencia recursivo. Cuando el precio proyectado se mueve a
al menos 15 pips más allá del precio de mercado actual, el EA entra en esa dirección con un stop-loss distante y una toma de ganancias en el
nivel proyectado. La conversión StockSharp reproduce el mismo filtro y lógica de decisión mientras se basa en la vela de alto nivel.
API para que cada cálculo se realice en barras terminadas.

## Lógica comercial
1. Suscríbase al período de tiempo seleccionado por `CandleType` (predeterminado: velas de 1 hora) y procese solo velas completadas.
2. Mantenga un historial continuo de precios de cierre lo suficientemente grande como para cubrir el máximo de `CountBuy` y `CountSell` más una barra adicional.
3. Recree la función MetaTrader `Out()`: calcule promedios móviles ponderados lineales y simples (usando un desplazamiento de una barra), obtenga el
componentes recursivos `s` y `t` con el precio configurado y los factores de tendencia, y obtener el precio proyectado `s[1] + t[1]`.
4. Para configuraciones largas (`EnableBuy`):
   - Compruebe que el precio proyectado esté al menos `15` pips por encima del último cierre (`Bid + 15*x*Point` en MT4).
   - Si no hay ninguna posición larga abierta, compre el volumen configurado (consulte `BaseVolume` y `BuyRiskMultiplier`).
   - Almacene el precio proyectado como toma de ganancias y calcule el límite de pérdidas restando `BuyStopLossPips` convertido en pasos de precio.
5. Para configuraciones breves (`EnableSell`):
   - Exija que el precio proyectado se sitúe al menos `15` pips por debajo del último cierre.
   - Si no hay ninguna posición corta abierta, venda el volumen configurado (revirtiendo una posición larga existente si es necesario).
   - Guarde el precio proyectado como toma de ganancias y establezca el límite de pérdidas `SellStopLossPips` pips por encima del mercado.
6. Mientras exista una posición, monitoree cada vela terminada:
   - Salir de posiciones largas si el mínimo de la vela cruza el stop almacenado o el máximo alcanza la toma de ganancias.
   - Salga de los cortos si el máximo de la vela cruza el stop o el mínimo alcanza el objetivo.
   - Las salidas utilizan `ClosePosition()`, lo que aplana la exposición neta en StockSharp.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `EnableBuy` | `bool` | `true` | Permita que la estrategia abra posiciones largas. |
| `CountBuy` | `int` | `24` | Longitud del filtro suavizante utilizado para proyecciones largas. |
| `BuyPriceFactor` | `decimal` | `0.18` | Peso del cierre actual en el filtro recursivo largo. |
| `BuyTrendFactor` | `decimal` | `0.18` | Ponderación aplicada al término de tendencia al construir la proyección larga. |
| `BuyStopLossPips` | `int` | `62` | Distancia de stop-loss para posiciones largas, medida en pips. |
| `BuyRiskMultiplier` | `decimal` | `0` | Multiplicador aplicado a `BaseVolume` antes de enviar una orden larga (0 mantiene el volumen base). |
| `EnableSell` | `bool` | `true` | Permita que la estrategia abra posiciones cortas. |
| `CountSell` | `int` | `24` | Longitud del filtro suavizante utilizado para proyecciones cortas. |
| `SellPriceFactor` | `decimal` | `0.18` | Peso del cierre actual en el filtro recursivo corto. |
| `SellTrendFactor` | `decimal` | `0.18` | Ponderación aplicada al término de tendencia al construir la proyección corta. |
| `SellStopLossPips` | `int` | `62` | Distancia de stop-loss para posiciones cortas, medida en pips. |
| `SellRiskMultiplier` | `decimal` | `0` | Multiplicador aplicado a `BaseVolume` antes de enviar una orden corta (0 mantiene el volumen base). |
| `BaseVolume` | `decimal` | `1` | Tamaño de orden base utilizado cuando ambos multiplicadores de riesgo son cero. |
| `CandleType` | `DataType` | plazo de 1 hora | Serie de velas procesadas por la estrategia. |

## Diferencias con el original MetaTrader EA
- MetaTrader puede mantener entradas de compra y venta independientes simultáneamente. StockSharp utiliza posiciones netas, por lo que el puerto cierra o
invierte una posición existente antes de abrir el lado opuesto.
- La función de lote MT4 utilizaba margen libre de cuenta. La conversión expone `BaseVolume` y dos multiplicadores de riesgo; cuando son cero
el volumen base se usa tal cual; de lo contrario, el volumen simplemente se escala (`BaseVolume * RiskMultiplier`).
- Los niveles de stop-loss y take-profit se ejecutan monitoreando las velas completadas. Por lo tanto, los rellenos intrabarra pueden diferir de MetaTrader
donde las órdenes de protección son gestionadas por el corredor.
- El ajuste de cinco dígitos `Digits`/`Point` se emula inspeccionando `Security.Decimals` y `Security.PriceStep` para convertir pip.
distancias a precios absolutos.
- Todos los cálculos de los indicadores se realizan en código administrado sin llamar a `iMA`; el filtro recursivo se recrea en
`CalculateTarget` usando los mismos coeficientes que la función MQL.

## Notas de uso
- Asigne el instrumento deseado a `Strategy.Security` antes de comenzar. La estrategia genera una excepción si no se adjunta ningún valor.
- Configure `BaseVolume` para que coincida con el tamaño del contrato esperado por su lugar; ajuste los multiplicadores de riesgo solo si desea escalar
la exposición relativa al volumen base.
- El historial de velas debe contener al menos `max(CountBuy, CountSell) + 1` barras antes de que se pueda generar cualquier operación. Proporcionar suficiente
datos de calentamiento o iniciar la estrategia con velas históricas cargadas.
- El búfer de entrada de 15 pips es fijo (al igual que en EA). Aumente `CountBuy`/`CountSell` para suavizar la proyección o modificar la
factores de precio/tendencia para que coincidan con el comportamiento observado en MetaTrader.
- Debido a que las salidas dependen de los extremos de las velas, habilite un período de tiempo que se adapte a su latencia de ejecución. Los plazos más cortos reaccionarán antes
pero requieren más datos históricos y pueden generar más señales.

## Detalles de implementación
- Utiliza `SubscribeCandles()` con `Bind(ProcessCandle)` para que cada decisión se base en velas finalizadas.
- Mantiene una lista compacta de cierres recientes y reconstruye el filtro recursivo `s`/`t` a pedido, imitando la rutina `Out()`.
- Convierte compensaciones basadas en pips a través del tamaño de tick del instrumento y la precisión decimal para replicar la escala MetaTrader `x * Point`.
- `ClosePosition()` se invoca cuando se superan los niveles de protección, lo que garantiza que la posición neta se nivele antes de que se realice otra entrada.
considerado.
