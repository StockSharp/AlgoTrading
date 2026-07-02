# Estrategia OpenTiks
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia OpenTiks traslada el clásico MetaTrader asesor experto `OpenTiks.mq4` al ecosistema StockSharp. El robot original
Buscó una escalera de velas con máximos estrictamente monótonos y se abre para detectar rupturas tempranas. Una vez que surgió una señal,
abrió una orden de mercado, opcionalmente adjuntó un stop de protección y luego siguió la posición mientras tomaba ganancias progresivamente
reduciendo repetidamente a la mitad la exposición. La versión StockSharp refleja esas ideas mediante llamadas de alto nivel API, suscripciones de velas,
y los asistentes de orden integrados para que la lógica se ejecute dentro de Designer, Runner o cualquier aplicación S# personalizada.

## Detección de patrones
Se puede iniciar una operación cuando **cuatro velas consecutivas** satisfacen uno de los siguientes patrones:

- **Ruptura alcista** – para la vela actual y las tres barras anteriores: cada `High` es estrictamente más alto que el anterior
`High`, y cada `Open` es estrictamente superior al `Open` anterior.
- **Ruptura bajista** – para la misma ventana de cuatro barras: cada `High` es estrictamente más bajo que el `High` anterior, y cada `Open`
es estrictamente inferior al anterior `Open`.

Las señales se evalúan en velas completas entregadas por el `CandleType` configurado. Cuando se cumple la condición de ruptura,
La estrategia envía una orden de mercado con el volumen configurado (normalizado al `VolumeStep` del valor y delimitado por `MinVolume`
y `MaxVolume`). El parámetro `MaxOrders` limita cuántas entradas simultáneas pueden existir; un valor de cero desactiva la verificación,
mientras que cualquier número positivo bloquea nuevas operaciones una vez que la posición neta absoluta dividida por el volumen de orden normalizado alcanza ese
límite.

## Gestión de riesgos y salidas.
- **Stop loss**: si `StopLossPoints` es mayor que cero, la estrategia monitorea la última vela para detectar reversiones de precios. largo
las posiciones se liquidan cuando el mínimo de la vela penetra `entryPrice - StopLossPoints × PriceStep`. Las posiciones cortas salen cuando
el alto toca `entryPrice + StopLossPoints × PriceStep`.
- **Trailing stop**: una vez que el precio avanza al menos `TrailingStopPoints × PriceStep` más allá de la entrada, se activa un trailing stop
a la misma distancia detrás (para largos) o delante (para cortos) del cierre. Cada vez que mejora el nivel de seguimiento, el
La posición restante se reduce opcionalmente.
- **Toma de ganancias progresiva**: cuando `UsePartialClose` está habilitado, la estrategia cierra la mitad de la exposición actual cada vez
el trailing stop avanza. Los volúmenes se redondean al `VolumeStep` del instrumento. Si el tamaño reducido a la mitad cae por debajo
`MinVolume`, toda la posición se cierra, coincidiendo con el comportamiento del experto MetaTrader.

Todos los cálculos de stop y trailing se realizan en velas terminadas, por lo que las salidas se producen en el siguiente cierre de barra en lugar de cada
tic entrante. Esto mantiene la implementación consistente con el nivel alto API de StockSharp mientras se mantiene cerca del original.
idea de reaccionar ante nuevos bares.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `OrderVolume` | `decimal` | `0.1` | Tamaño de lote base para cada entrada al mercado. La estrategia lo normaliza según el nivel y los límites del volumen del valor. |
| `StopLossPoints` | `decimal` | `0` | Distancia de parada de protección expresada en puntos de precio (pasos de precio). Un valor de cero desactiva la parada. |
| `TrailingStopPoints` | `decimal` | `30` | Distancia mantenida por el trailing stop una vez que la posición pasa a ser rentable, también en puntos de precio. |
| `MaxOrders` | `int` | `1` | Número máximo de entradas abiertas simultáneamente. Zero elimina la restricción. |
| `UsePartialClose` | `bool` | `true` | Habilita la lógica de reducción a la mitad que bloquea las ganancias cada vez que avanza el trailing stop. |
| `CandleType` | `DataType` | `1 minute` período de tiempo | Suscripción de vela primaria utilizada para evaluación de señales y verificaciones de seguimiento. |

## Notas de implementación
- StockSharp funciona con **posiciones netas**, por lo que todas las órdenes para el valor configurado se acumulan en una sola posición larga o corta
exposición. Por lo tanto, el parámetro `MaxOrders` actúa sobre la posición agregada en lugar de sobre los tickets MetaTrader individuales.
- El seguimiento basado en velas significa que las comprobaciones de parada se realizan una vez por barra completada. Los operadores que necesitan protección a nivel de tick pueden reducir el
tamaño de vela o ampliar la lógica para suscribirse a operaciones.
- Los cierres parciales respetan los metadatos del instrumento (`VolumeStep`, `MinVolume`, `MaxVolume`) para evitar pedidos rechazados.
- Los comentarios en inglés en línea resaltan los principales puntos de decisión para que el archivo sirva también como material educativo al adaptar la idea.
a otros experimentos de fuga o de gestión del dinero.

## Consejos de uso
1. Seleccione un tipo de vela que coincida con el período de tiempo utilizado en la configuración original MetaTrader (por ejemplo, M1 o M5).
2. Verifique la configuración de pasos y lotes del instrumento; el `OrderVolume` predeterminado de `0.1` se adapta a los contratos de estilo Forex, pero puede ser
ajustado por futuros, acciones o símbolos criptográficos.
3. Experimente con `TrailingStopPoints` y `UsePartialClose` para encontrar un equilibrio entre el bloqueo agresivo de ganancias y el alquiler.
los ganadores corren.
4. Combine la estrategia con gráficos StockSharp para confirmar visualmente el patrón de la escalera y observar las salidas parciales en tiempo real.
tiempo.
