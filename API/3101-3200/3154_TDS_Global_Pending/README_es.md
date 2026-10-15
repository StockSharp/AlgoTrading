# Estrategia TDS Global Pending (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia porta el asesor experto de MetaTrader 5 **TDSGlobal** de `MQL/23255/TDSGlobal.mq5` a la API de alto nivel de StockSharp. Evalúa el Momentum en velas de cuatro horas a través de la línea MACD, el histograma MACD (OsMA) y el Índice de Fuerza. Cuando la combinación de indicadores señala una posible reversión, la estrategia envía órdenes límite pendientes alrededor de los extremos de la vela anterior y gestiona la posición resultante con lógica opcional de stop-loss, take-profit y trailing-stop.

La implementación reproduce el flujo de trabajo original adaptándolo a construcciones idiomáticas de StockSharp como `StrategyParam<T>`, suscripciones de velas mediante `SubscribeCandles` y manejo asíncrono de órdenes a través de los eventos del ciclo de vida de la estrategia.

## Lógica de trading

1. **Cálculo de indicadores**
   - `MACD(12, 26, 9)` proporciona tanto la línea MACD como el histograma (OsMA).
   - `ForceIndex(24)` mide la fuerza de la última vela completada.
   - Cada indicador se actualiza al cierre del tipo de vela seleccionado (por defecto: 4 horas).
2. **Detección de señales**
   - El algoritmo espera hasta que estén disponibles dos valores históricos de MACD y OsMA para determinar su pendiente.
   - Una configuración de *venta* requiere que OsMA aumente (`osma[1] > osma[2]`) mientras que el Índice de Fuerza de la vela anterior sea negativo.
   - Una configuración de *compra* requiere que OsMA disminuya (`osma[1] < osma[2]`) mientras que el Índice de Fuerza anterior sea positivo.
3. **Colocación de órdenes**
   - Las órdenes límite de venta se colocan ligeramente por encima del máximo de la vela anterior; las órdenes límite de compra ligeramente por debajo del mínimo de la vela anterior.
   - Si el precio no está suficientemente lejos del bid/ask actual, el precio de la orden se ajusta al búfer de compensación configurado (`EntryOffsetPips`, por defecto 16 pips).
   - La estrategia verifica que la distancia entre el precio de la orden y el bid/ask actual supere la aproximación del nivel de seguridad del bróker (`MinDistancePips` o el valor dinámico basado en el spread).
4. **Controles de riesgo**
   - Los niveles opcionales de stop-loss y take-profit se calculan desde el precio de la orden.
   - Cuando una posición está activa, un trailing stop puede avanzar por el paso configurado una vez que el precio supera la distancia de trailing inicial.
   - Si el precio alcanza los niveles de protección dentro de una vela, la posición se cierra con una orden de mercado para imitar el comportamiento de MetaTrader.
5. **Mantenimiento de órdenes**
   - Las órdenes pendientes se cancelan cuando la pendiente de OsMA se vuelve contra la configuración original, coincidiendo con la rutina de limpieza del EA fuente.
   - El llenado de un lado cancela automáticamente la orden pendiente opuesta para evitar exposiciones conflictivas.

## Gestión de capital

Dos enfoques de dimensionamiento de posición están disponibles:

- **Volumen fijo** (por defecto `OrderVolume = 1`) — usa el `Strategy.Volume` base sin ajustes.
- **Dimensionamiento basado en riesgo** — cuando `UseRiskSizing` está habilitado, la estrategia estima el patrimonio del portafolio, convierte el porcentaje de riesgo configurado en riesgo en moneda y lo divide por la distancia del stop-loss para derivar el volumen de la orden. Los volúmenes se alinean al paso de volumen del instrumento para evitar tamaños de orden inválidos.

## Parámetros

| Nombre | Descripción | Por defecto |
| --- | --- | --- |
| `OrderVolume` | Tamaño de orden fijo cuando el dimensionamiento por riesgo está deshabilitado. | 1 |
| `UseRiskSizing` | Habilitar gestión de capital basada en `RiskPercent`. | true |
| `RiskPercent` | Porcentaje del patrimonio del portafolio arriesgado por operación. | 3 |
| `MacdFastPeriod` | Longitud de la EMA rápida para la línea MACD. | 12 |
| `MacdSlowPeriod` | Longitud de la EMA lenta para la línea MACD. | 26 |
| `MacdSignalPeriod` | Longitud de la EMA de señal para el histograma MACD. | 9 |
| `ForceLength` | Longitud de suavizado EMA para el Índice de Fuerza. | 24 |
| `StopLossPips` | Distancia del stop-loss en pips (0 deshabilita). | 50 |
| `TakeProfitPips` | Distancia del take-profit en pips (0 deshabilita). | 50 |
| `TrailingStopPips` | Distancia del trailing stop en pips (0 deshabilita). | 5 |
| `TrailingStepPips` | Paso mínimo para actualizaciones de trailing. | 5 |
| `EntryOffsetPips` | Búfer añadido alrededor de máximos/mínimos anteriores para órdenes pendientes. | 16 |
| `MinDistancePips` | Distancia mínima permitida entre el precio y los niveles de protección. | 3 |
| `PipSize` | Tamaño del pip usado para conversiones pip-a-precio. | 0.0001 |
| `CandleType` | Tipo de vela procesado por la estrategia. | Velas de 4 horas |

## Notas de uso

1. Añade el archivo `CS/TdsGlobalPendingStrategy.cs` a tu proyecto StockSharp o cárgalo dinámicamente a través del entorno de Backtester.
2. Asigna el instrumento y portafolio deseados antes de iniciar la estrategia. Si `UseRiskSizing` está habilitado, asegúrate de que el portafolio proporcione valores de patrimonio actuales.
3. La estrategia requiere al menos dos velas completadas para inicializar las pendientes de MACD/OsMA. Se espera una breve fase de calentamiento.
4. Monitorea los logs para eventos detallados de órdenes y posiciones. La implementación registra las acciones clave (envío de órdenes, cancelación, actualizaciones de trailing) para facilitar la verificación contra el comportamiento del EA original.

## Diferencias con la versión MQL

- La API de alto nivel gestiona eventos de órdenes asíncronos, por lo que los llenados de órdenes límite se manejan mediante `OnOwnTradeReceived` en lugar de resultados síncronos de `OrderSend`.
- Los niveles de "congelamiento" y "stops" del bróker se aproximan usando la distancia mínima configurada y una heurística basada en el spread porque StockSharp no expone límites de trading específicos de MetaTrader.
- Las salidas protectoras se ejecutan mediante órdenes de mercado cuando la vela muestra una ruptura. Esto replica la lógica de modificación manual de stop del EA sin depender de las restricciones del servidor de trading MT5.

Estos ajustes mantienen la lógica de trading fiel mientras aseguran que la estrategia se integre sin problemas con el framework de StockSharp.
