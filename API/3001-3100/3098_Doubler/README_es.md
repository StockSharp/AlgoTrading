# Estrategia de Doblador con Hedge y Trailing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de Doblador con Hedge y Trailing** es una conversión directa a la API de alto nivel de StockSharp del expert advisor de MetaTrader 5 `Doubler.mq5`. El algoritmo abre inmediatamente una posición de mercado larga y corta simétrica siempre que no exista exposición, luego gestiona ambas patas con reglas independientes de stop-loss, take-profit y trailing stop. La conversión preserva el comportamiento de hedging del programa MQL original mientras adapta la gestión del riesgo a las primitivas de StockSharp (órdenes de mercado, suscripciones Level1 y parámetros de estrategia).

A diferencia de las estrategias direccionales, el sistema mantiene ambas direcciones activas hasta que cada pata sale por su propia lógica protectora. Una vez que *ambas* patas están planas, la estrategia recrea el hedge, manteniendo continuamente la exposición emparejada.

## Características clave
- **Hedging automático** – abre una orden de compra y venta con el mismo volumen siempre que la estrategia no tenga posiciones activas.
- **Controles de riesgo basados en pips** – stop-loss, take-profit y offsets de trailing se configuran en pips y se convierten internamente a pasos de precio inspeccionando el paso de precio del valor y la precisión decimal (los instrumentos de 3/5 decimales se escalan automáticamente por un factor de 10).
- **Trailing independiente por pata** – cada pata rastrea el mejor bid/ask actual. Cuando el precio se mueve más de `TrailingStopPips + TrailingStepPips` a favor, el nivel de stop se avanza en `TrailingStopPips` respetando la condición del paso de trailing, reflejando exactamente la lógica del EA original.
- **Validación de volumen** – el volumen de la orden se valida contra `MinVolume`, `MaxVolume` y `VolumeStep`, generando una excepción cuando el tamaño solicitado viola las restricciones del intercambio.
- **Diagnósticos opcionales** – la bandera `LogTradeDetails` habilita mensajes informativos detallados (entradas, salidas, ajustes de trailing) que ayudan durante las pruebas o el monitoreo en vivo.

## Parámetros
| Parámetro | Descripción | Predeterminado | Notas |
|-----------|-------------|----------------|-------|
| `OrderVolume` | Volumen de cada pata del hedge (órdenes de compra y venta). | `1` | Debe respetar los límites de volumen del intercambio; normalizado al `VolumeStep` más cercano. |
| `StopLossPips` | Distancia del stop-loss en pips. | `150` | `0` desactiva el stop-loss. |
| `TakeProfitPips` | Distancia del take-profit en pips. | `300` | `0` desactiva el take-profit. |
| `TrailingStopPips` | Distancia del trailing stop en pips. | `5` | Si es mayor que cero, `TrailingStepPips` también debe ser positivo. |
| `TrailingStepPips` | Movimiento adicional mínimo antes de que avance el trailing stop. | `5` | Barrera que evita que el stop se mueva con demasiada frecuencia. |
| `LogTradeDetails` | Habilita el registro detallado de ejecuciones y actualizaciones de trailing. | `false` | Establecer en `true` para ejecuciones de depuración. |

## Lógica de trading
### Entrada
1. Suscribirse a actualizaciones Level1 (mejor bid/ask).
2. Cuando tanto `_longPosition` como `_shortPosition` son nulos y no hay órdenes de entrada pendientes, registrar dos órdenes de mercado: una de compra y una de venta con `OrderVolume` cada una.
3. Después de confirmar las ejecuciones, la estrategia registra los precios de entrada, los niveles iniciales de stop/take y reinicia los rastreadores de trailing.

### Gestión del riesgo
- **Stop-loss** – para cada pata el stop inicial se coloca a `StopLossPips` de distancia del precio de entrada. Una distancia de stop de `0` desactiva completamente el stop protector.
- **Take-profit** – take-profit simétrico en `TakeProfitPips`. Un valor de `0` desactiva los objetivos de beneficio.
- **Cierre forzado** – si `NormalizeVolume` detecta un tamaño no válido (demasiado pequeño/grande o que no coincide con `VolumeStep`), la estrategia genera una excepción para evitar enviar una orden inválida.

### Comportamiento del trailing stop
1. Cuando el precio se mueve favorablemente al menos `TrailingStopPips + TrailingStepPips`, el stop avanza a `currentPrice ± TrailingStopPips`.
2. La verificación del paso de trailing reproduce la condición MQL: el stop solo se mueve si el nuevo nivel está al menos `TrailingStepPips` más cerca del precio que el stop existente, o si todavía no existe stop.
3. Para posiciones largas se usa el mejor bid como precio de referencia; para posiciones cortas se usa el mejor ask para que los niveles de salida reflejen precios de ejecución realistas.

### Salida
- Cada pata sale de forma independiente cuando se cumple su condición de stop-loss, trailing stop o take-profit. Las órdenes de salida se registran como órdenes de mercado, y una vez que una pata está plana, su estado interno se limpia.
- Después de que ambas patas se cierran, la próxima actualización Level1 desencadena un nuevo par con hedge.

## Requisitos de datos
- **Level1 (mejor bid/ask)** – necesario para instantáneas del precio de entrada, cálculos de trailing y activadores de salida.
- No es necesaria ninguna suscripción de velas o trades; la estrategia reacciona exclusivamente a actualizaciones Level1.

## Notas sobre la conversión
- Las distancias en pips se convierten a offsets de precio absolutos multiplicando por el `PriceStep` del valor. Los instrumentos cotizados con 3 o 5 decimales reciben automáticamente un ajuste ×10, coincidiendo con la definición de pip usada en el expert de MetaTrader.
- La estrategia se basa en los métodos de `Strategy` de alto nivel de StockSharp (`RegisterOrder`, `StartProtection`, `SubscribeLevel1`) y evita las operaciones de conector de bajo nivel.
- El hedging se implementa a través de objetos `PositionState` internos para que las patas largas y cortas se rastreen incluso cuando el broker/portafolio usa posiciones netas.
- La conversión es autónoma y no modifica ni requiere ningún arnés de prueba del repositorio.
