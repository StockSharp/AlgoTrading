# Estrategia de Parabolic SAR EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Visión general
La **Estrategia de Parabolic SAR EA** es la conversión de alto nivel de StockSharp del asesor experto de MetaTrader `Parabolic SAR EA.mq5` ubicado en `MQL/23039`. El script MQL original reacciona a las reversiones del Parabolic SAR en un marco temporal configurable, abriendo posiciones de mercado con distancias de stop-loss y take-profit fijas expresadas en "pips" de MetaTrader (soporte de pip fraccional incluido). El port en C# se suscribe a velas, vincula el indicador `ParabolicSar` integrado y reproduce el mismo proceso de decisión barra a barra respetando las mejores prácticas de StockSharp.

## Lógica de Trading
1. **Preparación de datos**
   - La estrategia se suscribe al tipo de vela seleccionado por el usuario (velas de 30 minutos por defecto) y vincula un indicador Parabolic SAR configurado con paso de aceleración y valores máximos ajustables.
   - El valor SAR se calcula para cada vela y se entrega a la estrategia a través del callback de alto nivel `Bind`.
2. **Generación de señales**
   - Señal de compra: cuando el valor del Parabolic SAR de la vela terminada está estrictamente por debajo del mínimo de la vela.
   - Señal de venta: cuando el valor del Parabolic SAR de la vela terminada está estrictamente por encima del máximo de la vela.
   - Las señales se evalúan solo en velas completadas (`CandleStates.Finished`) para coincidir con el procesamiento de nueva barra de MQL.
3. **Gestión de posición**
   - La exposición opuesta se aplana antes de una nueva entrada aumentando el tamaño de la orden de mercado solicitada con el valor absoluto de la posición actual, replicando la secuencia de MetaTrader `ClosePosition` más `OpenPosition`.
   - Cada entrada recalcula los niveles de stop-loss y take-profit de protección usando las mismas reglas de conversión pip-a-precio que MetaTrader (los instrumentos de 3/5 dígitos reciben un multiplicador ×10 para el `PriceStep`).
4. **Salidas de protección**
   - En cada vela terminada la estrategia verifica si el máximo/mínimo viola el nivel de stop-loss o take-profit almacenado. Si se activa, la posición se cierra con una orden de mercado y los objetivos correspondientes se borran.
   - La lógica de protección se activa antes de las nuevas señales en la misma barra, reflejando el comportamiento original del Asesor Experto donde las órdenes stop son del lado del broker.

## Notas sobre Indicador y Datos
- Usa el indicador `ParabolicSar` integrado de StockSharp con parámetros `SarStep` y `SarMaximum`.
- La suscripción de velas se maneja a través de `SubscribeCandles` sin agregar el indicador a `Strategy.Indicators`, según lo requerido por las directrices del proyecto.
- El trading solo está permitido cuando `IsFormedAndOnlineAndAllowTrading()` reporta true, asegurando que los datos en vivo están presentes y el conector permite la presentación de órdenes.

## Parámetros
| Nombre | Por defecto | Descripción |
| --- | --- | --- |
| `TradeVolume` | `1` | Tamaño de la orden de mercado en lotes. Actualizar el valor también refresca `Strategy.Volume`. |
| `StopLossPips` | `50` | Distancia de stop-loss en pips de MetaTrader. Un pip equivale a `PriceStep × 10` para instrumentos con 3 o 5 decimales, de lo contrario solo `PriceStep`. Establecer en `0` para deshabilitar. |
| `TakeProfitPips` | `50` | Distancia de take-profit en pips de MetaTrader usando las mismas reglas de conversión que el stop-loss. Establecer en `0` para deshabilitar. |
| `SarStep` | `0.02` | Paso de aceleración usado por el indicador Parabolic SAR. |
| `SarMaximum` | `0.2` | Factor de aceleración máximo para el Parabolic SAR. |
| `CandleType` | `30m timeframe` | Tipo de vela usado para los cálculos. Soporta cualquier `DataType` derivado de `TimeFrame`. |

## Gestión de Riesgo y Comportamiento
- El stop-loss y take-profit se recalculan después de cada ejecución y se almacenan internamente; no se registran órdenes pendientes con el exchange.
- Si ambos niveles de protección se activan dentro de una sola vela, la verificación del stop-loss se activa primero, replicando el manejo conservador de la lógica MQL fuente.
- Cuando el conector no reporta un `PriceStep` válido, la conversión recurre a `0.0001` para evitar niveles de protección de distancia cero.
- No se realiza promediado ni piramidación; la estrategia opera con una única posición neta, cambiando de dirección cuando el Parabolic SAR cruza el precio.

## Notas de Conversión
- MetaTrader `InpBarCurrent` equivale a 1, lo que significa que el EA evalúa la vela terminada anterior. El port de StockSharp logra el mismo resultado procesando solo las velas `Finished` en el callback `Bind`.
- El asesor experto original usaba `CheckVolumeValue` para validar lotes y restricciones del broker. StockSharp delega estas verificaciones al conector, mientras que el parámetro `TradeVolume` aún impone un requisito de volumen positivo.
- La implementación de Python se omite intencionalmente, cumpliendo con los requisitos de la tarea.
