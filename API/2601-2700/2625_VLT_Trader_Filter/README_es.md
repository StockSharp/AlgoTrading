# Estrategia VLT Trader con Filtro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia VLT Trader con Filtro** es un sistema de ruptura por contracción de volatilidad convertido desde la implementación MQL original. Monitorea los rangos de candles recientes y prepara órdenes stop siempre que el candle completado más reciente se convierta en el menor rango dentro de una ventana histórica configurable. El objetivo es capturar movimientos explosivos después de un período de consolidación estrecha.

## Lógica de trading

1. **Procesamiento de nueva barra** – la estrategia evalúa las condiciones solo una vez por nuevo candle. El candle actual debe abrir por debajo del máximo del candle anterior para evitar operar gaps que saltan a través del nivel de ruptura.
2. **Filtro de volatilidad** – el rango del candle finalizado más reciente se compara con el menor rango entre los últimos `CandleCount` candles finalizados cuyo rango está por debajo de `MaxCandleSizePips`. Si el candle más reciente es estrictamente más pequeño, la configuración es válida.
3. **Colocación de entradas** – cuando la configuración es válida, se preparan dos órdenes stop:
   - Un **buy stop** `10` pips por encima del máximo anterior cuando la posición neta no es larga.
   - Un **sell stop** `10` pips por debajo del mínimo anterior cuando la posición neta no es corta.
   Las órdenes pendientes existentes del mismo tipo se cancelan antes de registrar nuevas.
4. **Gestión del riesgo** – una vez que una orden stop se activa y abre una posición, se adjuntan automáticamente órdenes de protección:
   - Take-profit en `TakeProfitPips` por encima/debajo del precio de entrada.
   - Stop-loss en `StopLossPips` por debajo/encima del precio de entrada.
   Las órdenes de protección se cancelan cuando la posición vuelve a cero.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `Volume` | Volumen de la orden enviado con cada orden stop. |
| `TakeProfitPips` | Distancia en pips usada para la orden take-profit después de la entrada. |
| `StopLossPips` | Distancia en pips usada para el stop de protección después de la entrada. |
| `MaxCandleSizePips` | Límite superior para los rangos de candles históricos considerados en el filtro de volatilidad. |
| `CandleCount` | Número de candles históricos usados para encontrar el rango mínimo aceptable. |
| `CandleType` | Marco temporal de candles usado para el análisis. |

## Notas de implementación

- El tamaño de pip se deriva del paso de precio del instrumento. Cuando el paso es menor o igual a `0.001`, se multiplica por `10` para emular la definición de pip de MetaTrader para instrumentos de 3 o 5 decimales.
- Los rangos de candles se almacenan en una cola FIFO limitada a `CandleCount` elementos, coincidiendo con el escaneo histórico realizado en el Expert Advisor original.
- Todas las órdenes se crean a través de la API de alto nivel de StockSharp (sin registro manual de órdenes) y se cancelan automáticamente cuando están obsoletas o cuando la posición se cierra.
- Los comentarios dentro del código están escritos en inglés, mientras que los archivos README proporcionan documentación multilingüe detallada.
