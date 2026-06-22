# Estrategia Gazonkos de Retroceso
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La Estrategia Gazonkos de Retroceso es una conversión del asesor experto original **gazonkos** de MetaTrader 5. El enfoque opera el gráfico horario del EUR/USD y busca un fuerte momentum entre dos cierres históricos. Después de detectar ese momentum, espera un retroceso de un tamaño predefinido y luego entra en la dirección del movimiento inicial. La implementación en StockSharp mantiene la misma máquina de estados por etapas que el código fuente mientras usa la API de alto nivel con suscripciones a velas y órdenes protectoras.

## Lógica de trading
1. **Verificación de elegibilidad** – solo se permite una posición por hora. Si se abrió otra operación durante la misma hora del reloj, o si el número configurado de operaciones simultáneas ya está en ejecución, la estrategia espera.
2. **Detección de momentum** – compara los precios de cierre de dos velas pasadas (`SecondShift` menos `FirstShift`). Si la diferencia supera `Delta`, la estrategia registra la dirección prevista (largo si el cierre más nuevo es más alto, corto de lo contrario).
3. **Seguimiento del retroceso** – desde el momento en que aparece el momentum, el código monitorea el máximo más alto (para configuraciones largas) o el mínimo más bajo (para configuraciones cortas) alcanzado durante esa hora. Cuando el precio retrocede al menos `Rollback`, la configuración se vuelve elegible para ejecución. Si la hora cambia antes de que ocurra el retroceso, la señal se descarta.
4. **Ejecución de la orden** – una vez que se cumple la condición de retroceso, la estrategia coloca una orden de mercado con distancias fijas de take profit y stop loss. El dimensionamiento de la posición se controla a través del parámetro `TradeVolume`, y el helper integrado `StartProtection` gestiona las órdenes protectoras.

Esta secuencia refleja de cerca la versión MT5 que usó las variables `STATE` y `Trade` para coordinar el flujo de trabajo.

## Gestión del riesgo
* `StartProtection` configura distancias fijas de take profit y stop loss en unidades de precio absoluto, similar a cómo el experto adjuntaba TP/SL a cada orden.
* `ActiveTrades` limita la exposición total máxima comparando el valor absoluto de la posición con el producto del volumen configurado y el conteo de operaciones permitidas.
* La combinación de control por hora y confirmación de retroceso reduce el exceso de operaciones durante condiciones laterales.

## Parámetros
| Nombre | Predeterminado | Descripción |
| ------ | -------------- | ----------- |
| `TakeProfit` | `0.0016` | Distancia absoluta (en unidades de precio) para el take profit. Equivale a 16 puntos en una cotización de EUR/USD de 5 dígitos. |
| `Rollback` | `0.0016` | Retroceso requerido desde el extremo alcanzado después de la señal de momentum. |
| `StopLoss` | `0.0040` | Distancia absoluta para el stop loss protector. Equivalente a 40 puntos en EUR/USD. |
| `Delta` | `0.0040` | Diferencia mínima entre los dos cierres históricos que define un movimiento fuerte. |
| `TradeVolume` | `0.1` | Volumen de orden predeterminado pasado a `BuyMarket()` y `SellMarket()`. |
| `FirstShift` | `3` | Índice de barra más antigua (número de velas hacia atrás) usado para la comparación del precio de cierre. |
| `SecondShift` | `2` | Índice de barra más nueva usado en la comparación del precio de cierre. |
| `ActiveTrades` | `1` | Número máximo de operaciones simultáneas. Establecer en cero para deshabilitar el límite. |
| `CandleType` | Marco temporal de `1 hora` | Serie de velas usada para el análisis; por defecto velas horarias como el EA fuente. |

## Notas
* La estrategia funciona con cualquier instrumento que tenga un tamaño de tick razonable; ajustar `Delta`, `Rollback`, `TakeProfit` y `StopLoss` para que coincidan con el valor del punto del instrumento.
* Todos los comentarios en línea están escritos en inglés según lo requerido por las directrices del proyecto.
* Aún no se proporciona un port de Python para esta estrategia.
