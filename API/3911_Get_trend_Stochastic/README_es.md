# Obtener tendencia Stochastic Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia es una StockSharp versión de alto nivel del MetaTrader 4 asesor experto **Obtenga trend.mq4**. Evalúa el gráfico M15 para
entradas, valida la tendencia más amplia en el primer semestre y se basa en dos promedios móviles suavizados junto con un par de indicadores estocásticos.
osciladores para detectar rupturas de reversión a la media cerca de la tendencia a largo plazo. La implementación mantiene la gestión del dinero original.
reglas basadas en distancias fijas de toma de ganancias, stop-loss y trailing-stop expresadas en puntos de precio.

## Lógica comercial

1. **Indicadores y datos**
   - Las velas M15 alimentan un promedio móvil suavizado (SMMA, precio medio) con período `M15MaPeriod` y dos osciladores estocásticos.
   - Las velas H1 alimentan otra SMMA (precio medio) con el período `H1MaPeriod`.
   - El estocástico rápido (`FastStochasticPeriod`, 3, 3) proporciona la línea %K y su valor anterior. El estocástico lento (`SlowStochasticPeriod`, 3, 3) suministra la línea de señal %D.
2. **Configuración larga**
   - El cierre actual de M15 está por debajo de su SMMA y el cierre de H1 está por debajo de su propio SMMA.
   - La distancia entre el SMMA M15 y el cierre está dentro de `ThresholdPoints` pasos de precio.
   - Ambas líneas estocásticas están por debajo de 20. La línea rápida cruza por encima de la línea lenta durante la última vela (`fast` > `slow` mientras que el valor rápido anterior estaba por debajo de `slow`).
   - Si existe una posición corta, la estrategia primero compra suficiente volumen para aplanarla y luego abre una nueva posición larga con `TradeVolume`.
3. **Configuración corta** refleja la lógica larga:
   - Ambos cierres se encuentran por encima de sus SMMA, la distancia está dentro de `ThresholdPoints`, los valores estocásticos están por encima de 80 y el rápido
La línea cruza por debajo de la línea lenta. La estrategia vende, cerrando un largo existente si es necesario.
4. **Gestión de riesgos**
   - Después de cada entrada, se colocan órdenes de protección en `StopLossPoints` y `TakeProfitPoints` (convertidas en precio absoluto
distancias utilizando el paso de precio del instrumento).
   - Un trailing stop realinea la orden de stop-loss una vez que la operación gana al menos `TrailingStopPoints` puntos. La nueva parada es
posicionado en el cierre actual menos/más la distancia de seguimiento para largos/cortos respectivamente.
   - Cuando la posición vuelve a estabilizarse, se cancelan todas las órdenes de protección.

## Diferencias versus el EA original

- El SMMA de MetaTrader utiliza un desplazamiento del indicador de ocho barras; Los indicadores StockSharp no exponen una configuración de cambio directo. el puerto
En su lugar, evalúa el valor final más reciente. Esto mantiene el tiempo de cruce y evita búferes personalizados adicionales.
- El EA original utilizó las cotizaciones de oferta y demanda de MQL para el seguimiento. El puerto utiliza el cierre de vela terminado que desencadenó el movimiento final.
actualización, que es el análogo más cercano disponible en el nivel alto API.
- La administración del dinero depende de los asistentes de registro de pedidos de StockSharp (`BuyMarket`, `SellMarket`, `SellStop`, etc.) en lugar de
`OrderSend` y `OrderModify`.

## Parámetros

| grupo | Nombre | Descripción | Predeterminado |
|-------|------|-------------|---------|
| Datos | `M15 Candle Type` | Tipo de vela/período de tiempo utilizado para los cálculos principales. | Marco de tiempo M15 |
| Datos | `H1 Candle Type` | Tipo de vela/plazo de tiempo utilizado para la confirmación. | Periodo H1 |
| Indicadores | `M15 SMMA Period` | Longitud de la media móvil suavizada en la serie M15. | 200 |
| Indicadores | `H1 SMMA Period` | Longitud de la media móvil suavizada en la serie H1. | 200 |
| Indicadores | `Slow Stochastic Period` | Longitud de %K para el oscilador estocástico lento que proporciona la línea %D. | 14 |
| Indicadores | `Fast Stochastic Period` | Longitud de %K para el oscilador estocástico rápido que proporciona la línea principal de %K. | 14 |
| Señales | `Threshold (points)` | Distancia máxima entre el M15 SMMA y el cierre actual para permitir entradas. | 50 |
| Riesgo | `Take Profit (points)` | Distancia de obtención de beneficios expresada en incrementos de precio. | 570 |
| Riesgo | `Stop Loss (points)` | Distancia de stop-loss expresada en pasos de precio. | 30 |
| Riesgo | `Trailing Stop (points)` | Distancia del trailing-stop expresada en incrementos de precio. | 200 |
| Comercio | `Trade Volume` | Volumen enviado con cada orden de mercado. | 0.1 |

## Notas de uso

- Asegúrese de que el valor comercializado exponga `PriceStep`; de lo contrario, las distancias basadas en puntos vuelven a `1`, lo que puede provocar grandes
órdenes de protección sobre instrumentos cotizados en unidades fraccionarias.
- La estrategia cancela y recrea órdenes stop tan pronto como se detecta un mejor nivel de seguimiento. Corredores que no permiten frecuentes
las modificaciones pueden requerir aceleración.
- Debido a que el puerto opera únicamente con velas terminadas, el sistema está diseñado para pruebas retrospectivas y ejecución de final de barra. Ejecutándolo
Los datos de ticks en vivo requieren hacer coincidir la configuración de creación de velas entre el terminal y StockSharp.
