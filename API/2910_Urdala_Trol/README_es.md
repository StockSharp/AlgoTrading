# Estrategia de Cuadrícula de Cobertura Urdala Trol
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de Cuadrícula de Cobertura Urdala Trol** es una conversión directa del asesor experto de MetaTrader 5 `Urdala_Trol.mq5` a la API de alto nivel de StockSharp. La estrategia mantiene continuamente exposición en ambas direcciones y escala posiciones usando una cuadrícula tipo martingala cuando se activan los stops. Opera enteramente con datos Level1 (mejor bid/ask) sin ningún indicador.

## Lógica de trading
1. **Cobertura inicial (Paso 0)** – cuando no hay posiciones activas, la estrategia abre inmediatamente una orden de mercado larga y una corta usando el parámetro *Base Volume*.
2. **Escalado del lado perdedor (Paso 1.2)** – si solo permanece abierta una dirección y la posición más perdedora en ese lado está al menos `Grid Step` pips del precio actual, la estrategia abre una posición adicional en la misma dirección. El nuevo volumen es igual al volumen de la posición menos rentable más `Min Lots Multiplier * minVolumeStep`, donde `minVolumeStep` se deriva del `VolumeStep` o `MinVolume` del instrumento.
3. **Manejo del stop-loss (Paso 1.1)** – cuando una posición es cerrada por el stop-loss (incluyendo ajustes de trailing) con resultado negativo, la estrategia vuelve a entrar en la misma dirección a menos que ya haya una operación activa a menos de `Min Nearest` pips del precio de salida.
4. **Reacción al stop rentable (Paso 2.1)** – cuando el stop cierra una posición con beneficio, la estrategia abre inmediatamente una operación en la dirección opuesta con el volumen escalado.
5. **Trailing stop** – una vez que el precio avanza `Trailing Stop + Trailing Step` pips más allá de la entrada, el stop se ajusta para mantener una distancia de `Trailing Stop` pips. El trailing es opcional y se aplica solo cuando ambos parámetros son mayores que cero.

Todas las distancias expresadas en pips se convierten a desplazamientos de precio absolutos a través del `PriceStep` del instrumento. Para cotizaciones de cinco o tres dígitos, la conversión multiplica el paso por diez para coincidir con la lógica "adjusted point" del MQL original.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| `BaseVolume` | 0.1 | Tamaño de lote inicial para abrir el primer par de cobertura. |
| `MinLotsMultiplier` | 3 | Número de lotes mínimos añadidos al volumen de la operación perdedora al escalar. |
| `StopLossPips` | 50 | Distancia del stop-loss en pips. Un valor de cero deshabilita el stop y la lógica de trailing. |
| `TrailingStopPips` | 5 | Distancia del trailing stop en pips. Establecer en cero para deshabilitar el trailing. |
| `TrailingStepPips` | 5 | Distancia adicional en pips requerida antes de que el trailing stop se mueva. Debe ser positivo cuando el trailing está habilitado. |
| `GridStepPips` | 50 | Distancia mínima de precio (en pips) entre la posición perdedora y el precio actual antes de colocar una nueva orden de escalado. |
| `MinNearestPips` | 3 | Si una posición existente está más cerca que esta distancia al último precio de stop, la estrategia omite la re-entrada inmediata. |

## Notas de implementación
- Usa `SubscribeLevel1()` para rastrear actualizaciones de bid/ask y ejecutar el motor de decisiones en cada tick.
- Las órdenes se registran mediante el asistente de alto nivel `RegisterOrder`, permitiendo seguimiento preciso a través de `OnOwnTradeReceived`.
- Los objetos de posición individuales se gestionan internamente para reproducir el comportamiento de cobertura, ya que los portafolios de StockSharp son de posición neta por defecto.
- La lógica de stop-loss y trailing se ejecuta dentro de la estrategia enviando órdenes de mercado una vez que se superan los umbrales; no se registran órdenes stop nativas.

## Consejos de uso
1. Asigne un instrumento líquido y un portafolio a la estrategia y asegúrese de que `PriceStep`, `VolumeStep` y los valores mínimos/máximos de volumen estén configurados para conversiones precisas.
2. Inicie la estrategia; construirá instantáneamente un par cubierto y luego reaccionará a los eventos de stop según la lógica MQL original.
3. Ajuste los parámetros de pips según la volatilidad del instrumento. Valores grandes de `Grid Step` reducen la frecuencia de órdenes adicionales, mientras que un `Min Lots Multiplier` mayor acelera el crecimiento martingala.
4. Monitoree la exposición resultante con cuidado; el comportamiento martingala puede escalar el volumen rápidamente cuando se activan múltiples stops consecutivos.

La implementación en Python no se proporciona intencionalmente en esta carpeta, según los requisitos de esta tarea de conversión.
