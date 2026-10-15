# Estrategia Trend Reversal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Trend Reversal es un sistema direccional que intenta capturar rupturas después de un retroceso de corto plazo dentro de una tendencia existente. Fue adaptada desde el asesor experto de MetaTrader "Trend Reversal" y reescrita para usar la API de alto nivel de StockSharp. La conversión conserva el bloque central de confirmación (medias móviles, momentum y MACD), sustituyendo los filtros gráficos de líneas originales por comprobaciones de solapamiento de precios más fáciles de reproducir programáticamente.

## Conjunto de indicadores
- **Medias móviles ponderadas lineales (LWMA)** sobre precio típico, con longitudes rápida y lenta personalizables. La línea rápida sigue el swing más reciente, mientras que la lenta identifica la tendencia dominante.
- **Oscilador Momentum** calculado en el mismo marco temporal. La estrategia registra la distancia absoluta desde el nivel neutral 100 para las tres últimas velas cerradas, emulando la lógica de MetaTrader.
- **Par de líneas de señal MACD** configurado con longitudes rápida, lenta y de señal independientes. La dirección del histograma se usa como confirmación de marco temporal superior para operaciones largas y cortas.

## Lógica de trading
1. Esperar una vela finalizada en el marco temporal configurado. La estrategia ignora barras parcialmente formadas.
2. Asegurar que ambas LWMAs y el indicador de momentum estén completamente formados. Sin historial suficiente, el sistema permanece plano.
3. Mantener una cola móvil de las tres desviaciones de momentum más recientes respecto a 100. Una configuración solo es válida si al menos uno de esos valores supera el umbral de compra o venta correspondiente.
4. Exigir que la vela de hace dos barras tenga un mínimo más bajo que el máximo de la vela anterior. Esto recrea la estructura "solapada" usada en el EA original para detectar una consolidación estrecha antes de la ruptura.
5. Evaluar filtros direccionales:
   - **Largo:** LWMA rápida por encima de LWMA lenta y valor principal MACD por encima de la línea de señal.
   - **Corto:** LWMA rápida por debajo de LWMA lenta y valor principal MACD por debajo de la línea de señal.
6. Respetar el límite de posición neta. La estrategia entra o añade a una posición solo cuando la exposición absoluta (posición actual dividida por volumen de operación) está por debajo del valor configurado `MaxPositions`.
7. Las órdenes se envían con `BuyMarket()` o `SellMarket()`, lo que permite giros parciales o completos según la exposición actual.

## Gestión de riesgos
- Las distancias opcionales de **take profit** y **stop loss** (expresadas en unidades de precio) pueden adjuntarse mediante el bloque de protección integrado de StockSharp. Ambos niveles se desactivan cuando un parámetro se establece en cero.
- Esta adaptación no incluye trailing stop automático ni ajuste de break-even. Estas funciones pueden implementarse con manejadores de eventos adicionales si se necesitan.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `CandleType` | Marco temporal principal usado para construir velas. | Marco temporal de 15 minutos |
| `FastLength` | Período de la LWMA rápida. | 6 |
| `SlowLength` | Período de la LWMA lenta. | 85 |
| `MomentumLength` | Período del oscilador momentum. | 14 |
| `MomentumBuyThreshold` | Desviación mínima absoluta de momentum (desde 100) que valida una configuración larga. | 0.3 |
| `MomentumSellThreshold` | Desviación mínima absoluta de momentum (desde 100) que valida una configuración corta. | 0.3 |
| `MacdFastLength` | Período de EMA rápida usado dentro del filtro MACD. | 12 |
| `MacdSlowLength` | Período de EMA lenta usado dentro del filtro MACD. | 26 |
| `MacdSignalLength` | Período de EMA de señal usado dentro del filtro MACD. | 9 |
| `TakeProfit` | Distancia de take profit en unidades de precio. Establecer en 0 para desactivar. | 50 |
| `StopLoss` | Distancia de stop loss en unidades de precio. Establecer en 0 para desactivar. | 20 |
| `TradeVolume` | Volumen de orden expresado en lotes. | 1 |
| `MaxPositions` | Número máximo de unidades de volumen de operación permitidas en la posición neta. | 1 |

## Notas de uso
- Adjunte la estrategia a un valor con información válida de paso y precio para que las órdenes de protección funcionen correctamente.
- Para trading multidireccional (piramidación o escalado), aumente `MaxPositions`. La estrategia seguirá añadiendo posiciones mientras los filtros sigan siendo válidos y la exposición permanezca dentro del límite.
- El backtesting debe realizarse con el mismo marco temporal de velas especificado por el parámetro `CandleType`. StockSharp solicitará automáticamente los datos adecuados cuando la estrategia inicie.
- Como la versión MetaTrader dependía de líneas de tendencia dibujadas a mano, esta reescritura sustituye esas comprobaciones por una condición determinista de solapamiento de velas. Esto mantiene el comportamiento consistente entre backtests y ejecución en vivo.

## Diferencias frente al EA original
- No se implementan trailing stop, movimientos a break-even ni salidas de emergencia basadas en equity para mantener el ejemplo centrado en la generación central de señales.
- Las funciones de gestión monetaria como multiplicación de lotes y filtrado por Magic Number no son necesarias en StockSharp y, por tanto, se eliminaron.
- La confirmación MACD usa el mismo marco temporal que las velas de trading en lugar de la agregación mensual original. Si se desea, se puede emular la configuración multitemporal suscribiéndose a un tipo de vela más lento y vinculando el filtro MACD a esa suscripción.

## Consejos de optimización
- Optimice primero las longitudes de las medias móviles para ajustarlas al ciclo dominante del mercado y después refine los umbrales de momentum.
- Experimente con distancias más amplias de stop-loss y take-profit al operar instrumentos volátiles. Dado que la lógica sigue tendencia, buffers de salida más grandes suelen mejorar la rentabilidad.
- Monitorice las estadísticas de drawdown durante las ejecuciones de optimización. Aumentar `MaxPositions` puede mejorar la respuesta, pero también amplifica el riesgo.
