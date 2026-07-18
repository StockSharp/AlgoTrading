# Estrategia Bill Williams Alligator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia porta el asesor experto de MetaTrader 5 **"Bill Williams.mq5"** de Vladimir Karputov a la API de alto nivel de StockSharp. Se suscribe a una única serie de velas, reconstruye los puntos fractales de Bill Williams y evalúa las rupturas en relación con las líneas Alligator desplazadas. Cuando la vela actual cierra más allá del fractal alcista o bajista más cercano y ese fractal se sitúa fuera de las tres curvas del Alligator (Mandíbula, Dientes, Labios), el sistema abre una posición. Las funciones opcionales de gestión monetaria reproducen los inputs originales como stop-loss, take-profit, trailing stop, reversión de señales y cierre automático de posiciones opuestas.

## Lógica de operación

1. **Detección de fractales** – cada vela finalizada actualiza los buffers rodantes de máximos y mínimos. El algoritmo escanea hasta `FractalsLookback` barras completadas y encuentra los fractales alcistas y bajistas de Bill Williams más recientes confirmados (patrón de cinco barras).
2. **Reconstrucción del Alligator** – el Precio Mediano `(High + Low) / 2` alimenta tres instancias de `SmoothedMovingAverage` que representan la mandíbula, los dientes y los labios. Sus valores se desplazan hacia adelante el número configurado de barras para coincidir con las reglas de representación de MetaTrader.
3. **Validación de ruptura** – una configuración larga requiere que el último fractal alcista esté por encima de la mandíbula, los dientes y los labios desplazados, mientras que la vela más reciente cierra por encima del precio del fractal. Una configuración corta refleja la lógica por debajo del Alligator.
4. **Ejecución de órdenes** – por defecto, la estrategia abre una única orden de mercado con `OrderVolume` cuando se detecta la ruptura y no se mantiene posición. Si `CloseOppositePositions` está habilitado, se aplana una posición opuesta antes de abrir una nueva. Establecer `ReverseSignals = true` intercambia los lados de ruptura para reproducir el modo inverso del EA.
5. **Gestión de riesgos** – los niveles de stop-loss y take-profit configurables se almacenan internamente y se evalúan en cada vela. El trailing stop se activa una vez que el mercado se mueve `TrailingStopPips + TrailingStepPips` en la dirección del trade y sigue avanzando conforme el precio avanza. Los stops se expresan en "pips" derivados del `PriceStep` del instrumento, incluyendo el ajuste de 3 o 5 dígitos de MetaTrader.

## Parámetros

| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `OrderVolume` | Tamaño del trade en lotes o contratos para entradas de mercado. | `0.1` |
| `StopLossPips` | Distancia de stop-loss inicial en pips. Establecer en `0` para deshabilitar. | `50` |
| `TakeProfitPips` | Distancia de take-profit en pips. Establecer en `0` para deshabilitar. | `50` |
| `TrailingStopPips` | Distancia de trailing stop en pips. `0` deshabilita la lógica de trailing. | `10` |
| `TrailingStepPips` | Ganancia pip adicional requerida antes de que el trailing stop se mueva nuevamente. Debe ser positivo cuando el trailing está habilitado. | `5` |
| `JawPeriod` | Longitud de la media móvil suavizada para la mandíbula del Alligator (azul). | `13` |
| `JawShift` | Desplazamiento hacia adelante para los valores de la mandíbula, medido en barras. | `8` |
| `TeethPeriod` | Longitud de la media móvil suavizada de los dientes (rojo). | `8` |
| `TeethShift` | Desplazamiento hacia adelante para los valores de los dientes. | `5` |
| `LipsPeriod` | Longitud de la media móvil suavizada de los labios (verde). | `5` |
| `LipsShift` | Desplazamiento hacia adelante para los valores de los labios. | `3` |
| `FractalsLookback` | Número de velas completadas escaneadas al buscar los fractales confirmados más recientes. | `100` |
| `ReverseSignals` | Cuando es `true`, las señales de compra provienen de rupturas de fractal bajista y las señales de venta provienen de rupturas de fractal alcista. | `false` |
| `CloseOppositePositions` | Cuando es `true`, la estrategia cierra una posición opuesta existente antes de entrar en un nuevo trade. | `false` |
| `CandleType` | Serie de velas utilizada para cálculos y señales. | `TimeFrame(1h)` |

## Notas

- La estrategia opera estrictamente en **velas finalizadas** e ignora los ticks intrabarra, coincidiendo con el flujo de trabajo barra a barra del Expert Advisor original.
- Para emular el cálculo de pip de MetaTrader 5, la estrategia multiplica el `PriceStep` del exchange por 10 cuando el instrumento tiene 3 o 5 lugares decimales.
- Las órdenes protectoras y el trailing stop se gestionan internamente. Cuando se cumple una condición de stop o objetivo en la siguiente vela, la posición se cierra a mercado para imitar la gestión de órdenes del EA.
- Los indicadores Alligator se dibujan automáticamente si hay un área de gráfico disponible, permitiendo la comparación visual entre el port de StockSharp y la plantilla de MetaTrader.
- Los proyectos de Python y pruebas se omiten intencionalmente según las directrices del repositorio para nuevas conversiones.
