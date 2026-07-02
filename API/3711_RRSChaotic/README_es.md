# Estrategia caótica de RRS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
El **RRS Chaotic EA** original tira continuamente los dados en cada tick, eligiendo símbolos aleatorios y tamaños de posición antes de enviar órdenes de mercado. El puerto StockSharp mantiene el espíritu de caos controlado al generar entradas desde un flujo de velas en la seguridad configurada. Cada vela cerrada desencadena una nueva decisión aleatoria tanto para la dirección como para el volumen, al tiempo que refleja las reglas de gestión del dinero del asesor experto.

## Características clave
- **Entradas aleatorias**: cada vela terminada genera un número entero aleatorio de 0 a 10. Los valores `6` o `9` abren una posición larga, mientras que `3` o `8` abren una posición corta, coincidiendo con la lógica MT4.
- **Volumen variable**: el volumen negociado se muestrea uniformemente entre los parámetros *MinVolume* y *MaxVolume* y se alinea con el paso de volumen del valor.
- **Filtro de spread**: las nuevas posiciones se bloquean siempre que el spread actual (en puntos) excede *MaxSpreadPoints*.
- **Take-profit y stop-loss**: salidas opcionales basadas en puntos que recrean la configuración del nivel de orden del experto.
- **Guardia de reducción**: las pérdidas no realizadas se comparan continuamente con un límite de efectivo fijo o con un porcentaje del valor de la cartera. La superación del límite cancela las órdenes activas y aplana la posición.

## Parámetros
| Nombre | Descripción |
|------|-------------|
| `CandleType` | Serie de velas utilizadas para activar la estrategia (velas predeterminadas de 1 minuto). |
| `MinVolume` / `MaxVolume` | Rango para generación de lotes aleatorios. |
| `TakeProfitPoints` | Distancia de obtención de beneficios en puntos de precio. Establezca en `0` para desactivar. |
| `StopLossPoints` | Distancia de stop-loss en puntos de precio. Establezca en `0` para desactivar. |
| `MaxOpenTrades` | Volumen neto máximo medido en pasos de volumen que pueden permanecer abiertos simultáneamente. |
| `MaxSpreadPoints` | Spread máximo permitido, expresado en puntos de precio. |
| `SlippagePoints` | Parámetro de deslizamiento informativo (se conserva para que esté completo). |
| `RiskControlMode` | Selecciona entre `FixedMoney` y `BalancePercentage` modelos de riesgo. |
| `RiskValue` | Ya sea la cantidad de dinero a arriesgar o el porcentaje de patrimonio, dependiendo de la modalidad. |
| `TradeComment` | Etiqueta adjunta a los pedidos generados para facilitar la auditoría. |

## Lógica de la estrategia
1. Suscríbase a la serie de velas configuradas y espere las velas terminadas.
2. Aplicar control de reducción. Si la pérdida no realizada supera el umbral, cancele las órdenes activas y cierre la posición actual.
3. Mantenga objetivos opcionales de stop-loss y take-profit que reflejen la configuración de la orden MT4.
4. Cuando se permita el comercio y el diferencial sea aceptable, obtenga un número aleatorio para decidir si abre una posición larga o corta.
5. Limite la exposición acumulada limitando el número de pasos de volumen a `MaxOpenTrades`.

## Diferencias frente a la versión MQL4
- El experto original negoció con múltiples símbolos aleatorios. Las estrategias StockSharp operan sobre un único valor; por lo tanto, la aleatoriedad se aplica únicamente a la dirección y al tamaño.
- Las paradas protectoras se ejecutan a través de órdenes de mercado al cierre de velas en lugar de parámetros nativos de órdenes de parada de pérdidas/toma de ganancias.
- La evaluación del diferencial utiliza la mejor oferta/demanda actual en lugar de la función MT4 `MarketInfo`.
- Todas las órdenes generadas incluyen el texto *TradeComment*, que proporciona un contexto similar a los números mágicos de MT4.

## Notas de uso
- Asegúrese de que la seguridad conectada exponga valores `PriceStep`, `MinStep` y `VolumeStep` válidos para una conversión precisa de punto a precio.
- El plazo de vela predeterminado es de un minuto para emular la aleatoriedad a nivel de tick sin abrumar el proceso de backtesting. Aumente el plazo para reducir la frecuencia de las operaciones.
- El control de riesgos se basa en las PnL no realizadas derivadas de la posición agregada. Las cestas mixtas largas/cortas, como se ve en la versión MT4, no son compatibles con StockSharp y, por lo tanto, se compensan.
