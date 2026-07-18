# Estrategia de inversión de SimpleTrade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- StockSharp puerto del asesor experto MetaTrader 4 **SimpleTrade.mq4** (también conocido como "neroTrade").
- Diseñado para operar con un solo símbolo en el período configurado mediante el parámetro `CandleType`.
- Siempre mantiene como máximo una posición abierta y cambia de dirección al abrir cada nueva barra.

## Lógica de trading
1. Cada vez que se activa una nueva vela, la estrategia compara el precio de apertura de la vela con el precio de apertura de la vela que tiene `LookbackBars` períodos más antigua.
2. Si la nueva apertura es estrictamente superior a la referencia histórica, todas las posiciones existentes se cierran y se envía una nueva orden larga de mercado con `TradeVolume` lotes.
3. De lo contrario (la apertura es igual o inferior), la estrategia cierra cualquier posición existente y abre una posición corta en el mercado del mismo tamaño.
4. El parámetro `StopLossPoints` refleja la configuración `stop` original de EA. Cuando tanto el `PriceStep` como el `StopLossPoints` del valor están disponibles, la estrategia convierte el valor en una distancia absoluta y lo reenvía a `StartProtection`, permitiendo que StockSharp mantenga las órdenes protectoras de stop-loss automáticamente.
5. Las aperturas de velas se rastrean mediante la suscripción de velas de alto nivel API. Las velas terminadas llenan la lista del historial, mientras que la vela activa activa la decisión una vez por barra.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `TradeVolume` | Tamaño base del pedido expresado en lotes. Debe ser positivo. | `1` |
| `StopLossPoints` | Distancia de parada de protección en los puntos del instrumento. Establezca en `0` para desactivar el stop-loss automático. | `120` |
| `LookbackBars` | Número de barras utilizadas para la comparación de precios abiertos. Un valor de `3` reproduce `Open[0]` frente a `Open[3]` del código original. | `3` |
| `CandleType` | Periodo de tiempo (como `DataType`) a partir del cual se solicitan velas. Controla cuando aparecen nuevas señales. | `1 hour timeframe` |

## Notas de implementación
- Utiliza el flujo de trabajo de alto nivel `SubscribeCandles(...).Bind(...)`, por lo que la estrategia sigue siendo liviana y reacciona tanto a velas históricas como a velas activas.
- `StartProtection` se invoca una vez durante `OnStarted`. Asegúrese de que la seguridad conectada proporcione `PriceStep`; de lo contrario, la distancia del stop-loss no se puede traducir a precios absolutos.
- Debido a que todas las operaciones se ingresan con órdenes de mercado al comienzo de cada barra, el manejo del deslizamiento se delega al centro de negociación y no hay ningún parámetro `slippage` adicional.
- El búfer abierto histórico mantiene solo una pequeña ventana móvil (valores `LookbackBars + 5`) para evitar el uso innecesario de memoria.
- No se proporciona ningún puerto Python; el directorio `CS/` contiene la única implementación.

## Estructura de archivos
```
4002_SimpleTrade/
├── CS/
│ └── SimpleTradeFlipStrategy.cs
├── README.md
├── README_zh.md
└── README_ru.md
```
