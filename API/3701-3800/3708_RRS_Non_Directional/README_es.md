# Estrategia no direccional RRS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia transfiere el asesor experto MetaTrader 4 "RRS no direccional" al marco StockSharp. El EA original abre cestas de compra y venta con cobertura según el modo de negociación seleccionado y las gestiona con reglas virtuales de stop-loss, take-profit y seguimiento. La implementación de StockSharp reproduce los modos configurables, el cierre de riesgo monetario y la lógica de protección virtual al tiempo que adapta el comportamiento a las carteras de compensación utilizadas por StockSharp. Por lo tanto, los modos basados ​​en coberturas alternan entre exposiciones largas y cortas en lugar de mantener posiciones opuestas simultáneas.

## Lógica comercial
- Suscríbase a los datos de Nivel 1 para leer los mejores precios de oferta y demanda. El diferencial informado por esas cotizaciones se compara con `MaxSpreadPoints` antes de cada decisión de entrada.
- Las entradas al mercado respetan el parámetro `TradingMode`:
  - `HedgeStyle` y `AutoSwap` reflejan el modo de doble cara al alternar entre operaciones largas y cortas (StockSharp no puede mantener boletos de compra y venta independientes simultáneamente).
  - `BuySellRandom` lanza una moneda en cada nueva oportunidad.
  - `BuySell` siempre abre el lado opuesto de la posición cerrada más recientemente.
  - `BuyOrder` y `SellOrder` restringen el comercio a una sola dirección.
- El externo `New_Trade` está asignado a `AllowNewTrades`, lo que proporciona una forma rápida de pausar todas las nuevas órdenes de mercado.
- Cada pedido utiliza el `TradeVolume` configurado y adjunta el `TradeComment` para facilitar el seguimiento por parte del corredor.

## Gestión de riesgos y salidas.
- Las distancias de stop-loss y take-profit se expresan en MetaTrader puntos. Se convierten a unidades de precio utilizando el instrumento `PriceStep` para que la lógica siga siendo independiente del corredor.
- `StopMode`, `TakeMode` y `TrailingMode` seleccionan entre gestión deshabilitada, virtual y clásica. En el puerto StockSharp ambos modos no deshabilitados se implementan como controles virtuales que cierran la posición mediante órdenes de mercado cuando se alcanza el umbral. Esto mantiene el comportamiento determinista entre conectores.
- La gestión de seguimiento se activa después de que el precio avanza en `TrailingStartPoints`, luego mantiene una parada dinámica que sigue el mejor precio en `TrailingGapPoints`.
- Las pérdidas y ganancias no realizadas se recalculan en cada actualización de Nivel 1. Cuando cae por debajo del umbral derivado de `RiskMode` y `MoneyInRisk`, la estrategia liquida la posición inmediatamente.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `TradingMode` | Selección de entrada copiada del EA original. Los modos de cobertura alternan entre operaciones largas y cortas según el modelo de compensación de StockSharp. |
| `AllowNewTrades` | Habilita o deshabilita nuevas órdenes de mercado. |
| `TradeVolume` | Tamaño base para pedidos. |
| `StopMode` | Manejo de stop-loss (`Disabled`, `Virtual`, `Classic`). |
| `StopLossPoints` | Distancia de stop-loss en MetaTrader puntos. |
| `TakeMode` | Manejo de toma de ganancias (`Disabled`, `Virtual`, `Classic`). |
| `TakeProfitPoints` | Distancia de obtención de beneficios en MetaTrader puntos. |
| `TrailingMode` | Gestión de trailing stop (`Disabled`, `Virtual`, `Classic`). |
| `TrailingStartPoints` | Ganancia (puntos) requerida antes de que se active el trailing stop. |
| `TrailingGapPoints` | Distancia (puntos) mantenida detrás del mejor precio una vez que el seguimiento está activo. |
| `RiskMode` | Interpreta `MoneyInRisk` como porcentaje de saldo o como monto de moneda absoluto. |
| `MoneyInRisk` | Monto o porcentaje de riesgo que desencadena una liquidación total cuando las pérdidas y ganancias flotantes caen por debajo del umbral. |
| `MaxSpreadPoints` | Spread máximo (puntos) permitido para nuevas operaciones. |
| `SlippagePoints` | Configuración de deslizamiento informativo mantenida para la paridad con las entradas originales. |
| `TradeComment` | Comentario adjunto a cada pedido. |

## Notas y limitaciones
- AutoSwap se basa en información de tasa de swap en MetaTrader. Los conectores StockSharp generalmente no exponen esas cifras a través de feeds de nivel 1, por lo que el modo vuelve a `HedgeStyle` y registra la degradación.
- Las opciones clásicas de stop-loss, take-profit y trailing se ejecutan virtualmente. Los corredores que requieren órdenes de protección nativas deben ser manejados mediante anulaciones de estrategias de nivel inferior.
- Debido a que StockSharp agrega posiciones por valor, la estrategia alterna la exposición en modos de cobertura en lugar de mantener dos tickets simultáneos. Este comportamiento está documentado para que las pruebas directas coincidan con las expectativas.
