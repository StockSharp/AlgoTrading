# Estrategia de Advanced EA Panel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un port de StockSharp del utilitario **Advanced EA Panel** de MQL5. El asesor experto original proporcionaba un panel de trading manual con análisis multi-marco temporal, gestión de pivotes y botones de operaciones rápidas. La implementación en C# recrea esas capacidades analíticas dentro de una estrategia automatizada para que permanezcan disponibles sin un panel de control en el gráfico.

## Características principales

- Agrega nueve marcos temporales (M1 … MN1) y rastrea votos de EMA(3/6/9), SMA(50/200), CCI(14) y RSI(21) para cada horizonte.
- Calcula niveles de pivote floor-trader, Woodie o Camarilla en una serie de velas configurable.
- Monitorea la volatilidad con un feed ATR y registra cada cambio significativo.
- Mantiene un panel de riesgo interno calculando la distancia del stop, la distancia de recompensa y la relación riesgo/recompensa en vivo para la posición activa.
- Admite ejecución automática de órdenes cuando el voto multi-marco temporal supera un umbral configurable. Las operaciones opuestas se aplastan antes de revertir, exactamente como al presionar los botones del panel.
- Aprovecha `StartProtection` para que los guardas de stop-loss y take-profit sobrevivan a los reinicios, reflejando la lógica de protección del panel original.

## Lógica de trading

1. Cada suscripción de marco temporal produce valores de indicadores para EMA(3/6/9), SMA(50/200), CCI(14) y RSI(21). Se agrega un voto alcista cuando el cierre está por encima de las medias móviles, CCI está por encima de +100 y RSI está por encima de 60. Los votos bajistas se producen para las condiciones opuestas. Las lecturas neutrales no contribuyen a la puntuación.
2. La puntuación total en los marcos temporales listos se compara con `DirectionalThreshold`. Las puntuaciones ≥ umbral generan una señal de **Compra**; las puntuaciones ≤ –umbral generan una señal de **Venta**.
3. Cuando el trading automático está habilitado, la estrategia:
   - Cierra la posición opuesta con `ClosePosition()` antes de enviar la orden de reversión.
   - Envía una orden de mercado dimensionada según `Volume`, redondeada al `Security.VolumeStep` más cercano.
   - Se basa en `StartProtection` para adjuntar brackets de stop-loss/take-profit expresados en pips.
4. El ATR de la serie de velas principal se registra. Cualquier cambio más allá de la precisión de redondeo imprime un nuevo informe de volatilidad.
5. Los niveles de pivote se recalculan cada vez que el marco temporal de pivote produce una vela finalizada. El registro muestra PP, R1–R4 y S1–S4 para que puedan usarse como niveles discrecionales o exportarse a dashboards.

## Parámetros

| Nombre | Descripción | Grupo | Predeterminado |
| --- | --- | --- | --- |
| `Volume` | Volumen de trading en lotes. Redondeado a `VolumeStep` antes de enviar órdenes. | Trading | 1.0 |
| `StopLossPips` | Distancia desde la entrada al stop-loss expresada en pasos de precio. `0` deshabilita el stop. | Riesgo | 50 |
| `TakeProfitPips` | Distancia desde la entrada al take-profit en pasos de precio. `0` deshabilita el take. | Riesgo | 100 |
| `VolatilityPeriod` | Longitud de lookback ATR usada para el registro de volatilidad. | Volatilidad | 14 |
| `PrimaryCandleType` | Tipo de vela que impulsa los cálculos ATR y el dibujo en gráfico. | General | Velas de 15 minutos |
| `PivotCandleType` | Tipo de vela usado para el recálculo de niveles de pivote. | General | Velas de 1 hora |
| `DirectionalThreshold` | Puntuación absoluta requerida para activar una señal de Compra/Venta. | Señales | 3 |
| `AutoTradingEnabled` | Habilita la ejecución automática de señales detectadas. | Señales | true |
| `PivotFormula` | Preset de pivote (`Classic`, `Woodie`, `Camarilla`). | General | Classic |

## Gestión de riesgos

- `StartProtection` adjunta brackets basados en precio calculados desde `StopLossPips` y `TakeProfitPips` (convertidos a precio absoluto usando `PriceStep`).
- `_entryPrice`, `_stopPrice` y `_takePrice` se actualizan en las ejecuciones para que la estrategia pueda registrar riesgo, recompensa y relación riesgo/recompensa en pips.
- Si el trading automático está deshabilitado, el monitor de riesgo sigue funcionando para entradas manuales ejecutadas fuera de la estrategia.

## Diferencias con el panel MQL5

- El EA original mostraba botones y líneas arrastrables en el gráfico; la versión de StockSharp expone los mismos análisis a través de registros y parámetros de estrategia. Todos los comentarios dentro del código explican cómo extender o conectar los resultados a una UI si es necesario.
- La gestión de posición está automatizada. Hacer clic en **Comprar**, **Vender**, **Revertir** o **Cerrar** es reemplazado por `RequestExecution`, `SendOrder` y `ClosePosition()` en reacción a la puntuación multi-marco temporal.
- Los puntos de interés, ediciones manuales de pestañas y manipulación de objetos en el gráfico no están portados. En cambio, los pivotes se recalculan programáticamente y se registran. Los traders pueden consumir el registro o extender la estrategia para dibujar objetos si lo desean.
- Las métricas de volatilidad, riesgo y pivotes persisten entre reinicios porque se recalculan a partir de datos en vivo en lugar de depender de objetos del gráfico.

## Notas de uso

1. Adjunte la estrategia a un símbolo y asegúrese de que el conector proporciona todos los tipos de velas listados en `PanelTimeFrames`. Los datos faltantes retrasarán la generación de señales hasta que se complete al menos una vela por marco temporal.
2. Ajuste `DirectionalThreshold` para controlar la sensibilidad. Umbrales más altos exigen más acuerdo entre marcos temporales antes de operar.
3. Configure `AutoTradingEnabled = false` para usar el módulo como dashboard informativo mientras coloca órdenes manualmente desde otra herramienta.
4. La clase agrega renderizado de gráfico predeterminado para velas primarias, ATR y operaciones propias. Elimine o extienda estas llamadas si se requiere una visualización personalizada.

## Resumen de conversión

- **Acciones de UI → Métodos de estrategia.** Los manejadores de botones del panel (`EAPanelClickHandler`, `T0ClickHandler`, etc.) se mapean a helpers de ejecución de órdenes que preservan el flujo de compra/venta/reversión/cierre.
- **Fórmulas de pivote.** Los selectores de MQL5 permitían fórmulas independientes por nivel; este port mantiene las combinaciones preset (`Classic`, `Woodie`, `Camarilla`) que el panel ofrecía mediante sus botones de selección rápida.
- **Seguimiento de indicadores.** Los handles de indicadores nativos de MQL5 son reemplazados por `ExponentialMovingAverage`, `SimpleMovingAverage`, `CommodityChannelIndex` y `RelativeStrengthIndex` de StockSharp con callbacks `Bind`.
- **Panel de riesgo.** Todos los cálculos de riesgo/recompensa que antes se renderizaban en cuadros de edición ahora se registran y pueden ser consumidos por cualquier componente de monitoreo.

La estrategia por lo tanto preserva la intención del Advanced EA Panel—conciencia situacional centralizada con lógica de reacción rápida—mientras se presenta como una estrategia de StockSharp completamente automatizada lista para optimización o monitoreo discrecional.
