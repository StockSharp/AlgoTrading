# Estrategia N Candles v6
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia **N Candles v6** monitorea las velas terminadas más recientes y busca rachas de dirección idéntica. Cuando el mercado imprime `N` velas alcistas seguidas, la estrategia abre una posición larga, mientras que una serie de `N` velas bajistas produce una entrada corta. La lógica está inspirada en el asesor experto de MetaTrader *N Candles v6.mq5* y se adapta a la API de alto nivel de StockSharp.

El algoritmo está diseñado para cualquier símbolo que entregue velas estándar basadas en tiempo. Una ventana de trading configurable mantiene la estrategia inactiva fuera de la sesión deseada, pero la lógica activa de trailing y salida continúa protegiendo una posición abierta incluso durante las horas bloqueadas.

## Lógica de trading
1. Suscribirse al tipo de vela configurado y procesar solo velas terminadas.
2. Contar velas consecutivas alcistas (`Close > Open`) y bajistas (`Close < Open`). Los dojis reinician los contadores.
3. Cuando aparecen `CandlesCount` velas alcistas:
   - Verificar que la posición neta proyectada permanezca por debajo de `MaxPositionVolume`.
   - Enviar una orden de compra de mercado. Si existe una posición corta, el tamaño de la orden se aumenta para voltear la posición a larga en un solo trade.
4. Cuando aparecen `CandlesCount` velas bajistas:
   - Asegurar que la nueva exposición corta no excederá `MaxPositionVolume`.
   - Enviar una orden de venta de mercado y ampliar la orden si se debe cerrar una posición larga.
5. Si la vela más nueva rompe la racha (la "oveja negra"):
   - Aplicar el `ClosingMode` seleccionado para cerrar todas las posiciones, las opuestas o las de la misma dirección una vez.
6. El trailing y las salidas protectoras se ejecutan en cada vela:
   - Los niveles de stop-loss y take-profit se derivan de distancias en pips y el paso de precio del instrumento.
   - El trailing stop se activa después de que el precio se mueve por `TrailingStopPips + TrailingStepPips` y solo se engancha en la dirección favorable.
   - Cualquier violación del stop, take-profit, o nivel de trailing cierra inmediatamente toda la posición.

## Gestión de riesgos
- **Stop Loss (pips)** – convierte la distancia en pips en un offset de precio absoluto usando el paso de precio del símbolo (los instrumentos de 5 y 3 dígitos se escalan automáticamente).
- **Take Profit (pips)** – cierra la posición después de un movimiento favorable del tamaño especificado.
- **Trailing Stop / Step (pips)** – habilita la protección dinámica una vez que el trade alcanza el umbral de ganancia configurado. El step debe ser distinto de cero cuando el trailing está activo.
- **Max Position Volume** – limita la posición neta absoluta. Las señales que infringirían el límite se ignoran.
- **Closing Mode** – determina cómo reaccionar cuando aparece una vela no conforme:
  - `All` – aplanar toda la posición.
  - `Opposite` – cerrar posiciones contra la dirección de la racha (p.ej., cerrar cortos después de que una racha alcista se rompe).
  - `Unidirectional` – cerrar posiciones solo en la dirección de la racha.
- **Ventana de trading** – la estrategia abre nuevos trades solo cuando la hora de apertura de la vela está entre `StartHour` y `EndHour` (inclusive). Las salidas protectoras continúan operando incluso cuando los nuevos trades están bloqueados.

## Parámetros
| Nombre | Por defecto | Descripción |
|--------|-------------|-------------|
| `CandlesCount` | 3 | Número de velas idénticas requeridas para una señal. |
| `OrderVolume` | 0.01 | Tamaño base de la orden de mercado. La exposición opuesta se cierra antes de establecer un nuevo trade. |
| `TakeProfitPips` | 50 | Distancia del take-profit en pips. `0` deshabilita el objetivo. |
| `StopLossPips` | 50 | Distancia del stop-loss en pips. `0` deshabilita el stop. |
| `TrailingStopPips` | 10 | Distancia del trailing stop en pips. `0` deshabilita el trailing. |
| `TrailingStepPips` | 4 | Mejora mínima de precio antes de que el nivel de trailing se mueva. Debe ser > 0 cuando el trailing está habilitado. |
| `MaxPositionVolume` | 2 | Máxima posición neta absoluta. |
| `UseTradingHours` | true | Habilita el filtrado de ventana de trading. |
| `StartHour` | 11 | Inicio de la sesión de trading (0-23). |
| `EndHour` | 18 | Fin de la sesión de trading (0-23). |
| `ClosingMode` | All | Comportamiento cuando aparece una vela oveja negra. |
| `CandleType` | Velas de 1 hora | Tipo de datos usado para la generación de señales. |

## Notas
- La conversión de pips se basa en el `PriceStep` del instrumento. Para cotizaciones de 5 y 3 dígitos, la estrategia multiplica el paso por diez para coincidir con la definición tradicional de pip.
- Llamar a `StartProtection()` durante el arranque para habilitar los servicios de salvaguarda de StockSharp (cancelar-al-stop, seguridad de reconexión, etc.).
- La lógica usa la posición neta (`Strategy.Position`) y por lo tanto opera correctamente en cuentas de neteo. El comportamiento estilo hedging se puede emular estableciendo un `MaxPositionVolume` alto.
