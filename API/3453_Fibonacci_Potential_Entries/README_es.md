# Fibonacci Estrategia de entradas potenciales
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia reproduce el comportamiento del asesor experto **EA_PUB_FibonacciPotentialEntries** original. Coloca dos órdenes límite en los niveles de retroceso del 50 % y 61 % Fibonacci y gestiona su ciclo de vida utilizando el nivel alto StockSharp API.

## Lógica de trading

1. **Colocación inicial**
   - Tan pronto como las cotizaciones de oferta/demanda estén disponibles, la estrategia calcula el diferencial actual y envía dos órdenes límite:
     - Orden #1: colocada en el nivel del 50% con un stop protector por debajo (o por encima para cortos) del nivel del 61%.
     - Orden #2: colocada en el nivel del 61% con un tope protector colocado a medio camino hacia el nivel del 100%.
   - Los volúmenes están dimensionados para que la primera operación arriesgue el 0,7% de la cartera y la segunda operación arriesgue la parte restante del parámetro `RiskPercent`.

2. **Manejo de objetivos**
   - Cuando el precio alcanza el nivel `TargetPrice`, la estrategia cierra la mitad de cada posición ocupada utilizando órdenes de mercado.
   - Después de una salida parcial, el volumen restante está protegido hasta el punto de equilibrio (precio de entrada). Si el mercado vuelve a ese nivel el resto de la posición se cierra automáticamente.

3. **Dirección**
   - `IsBullish = true` crea límites de compra (plantilla alcista original).
   - `IsBullish = false` refleja el comportamiento con límites de venta y controles de parada/objetivo invertidos.

## Parámetros

| Nombre | Descripción |
|------|-------------|
| `PriceOn50Level` | Nivel de precio para la primera orden límite. |
| `PriceOn61Level` | Nivel de precio para la segunda orden límite. |
| `PriceOn100Level` | Nivel de referencia utilizado para calcular la segunda parada comercial. |
| `TargetPrice` | Objetivo de beneficio compartido para ambas posiciones. |
| `RiskPercent` | Porcentaje total del capital de la cartera arriesgado en ambas entradas. |
| `IsBullish` | Elige entre configuraciones largas y cortas. |

## Notas de conversión

- Solo se utilizan ayudantes de alto nivel (`SubscribeLevel1`, `BuyLimit`, `SellLimit`, `BuyMarket`, `SellMarket`), exactamente como lo exigen las pautas del repositorio.
- Las salidas parciales y los ajustes de punto de equilibrio se reproducen con órdenes de mercado, coincidiendo con el comportamiento del robot MQL sin depender de llamadas de modificación de órdenes de bajo nivel.
- Los volúmenes de posición están normalizados al paso de volumen del instrumento para mantenerse consistentes con las convenciones StockSharp.
