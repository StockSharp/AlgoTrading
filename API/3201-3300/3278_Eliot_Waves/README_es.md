# Estrategia Eliot Waves
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia Eliot Waves replica el comportamiento del asesor experto de MetaTrader "Eliot Waves" usando la API de alto nivel de StockSharp. El algoritmo combina detección de tendencia mediante dos medias móviles ponderadas lineales con confirmación de momentum y salidas basadas en volatilidad. Todos los cálculos se realizan sobre velas cerradas de un marco temporal configurable para reflejar la ejecución determinista del robot original.

## Lógica de trading

1. **Filtro de tendencia.** La estrategia compara una LWMA rápida (período predeterminado 6) con una LWMA lenta (período predeterminado 85), calculadas sobre el precio típico de la vela. Las operaciones largas solo se consideran cuando la LWMA rápida cierra por encima de la LWMA lenta, mientras que las cortas requieren la alineación opuesta.
2. **Confirmación de momentum.** Un indicador de momentum (período predeterminado 14) debe mostrar al menos una de las tres últimas lecturas desviándose del valor neutral 100 por más que el umbral configurado (predeterminado 0.3). Esto replica el EA original, que comprobaba la diferencia absoluta de tres valores recientes de momentum.
3. **Filtro de estructura de vela.** Las señales largas requieren que el mínimo de la vela de hace dos barras esté por debajo del máximo de la vela anterior. Las señales cortas exigen que el mínimo de la vela anterior permanezca por debajo del máximo de dos barras atrás. Esto captura el filtro de estilo divergencia presente en el código fuente.
4. **Escalado de posición.** Cada señal intenta añadir un paso de volumen fijo (predeterminado 0.1) hasta el número máximo de pasos (predeterminado 10). La estrategia cierra cualquier exposición opuesta antes de abrir una nueva posición para mantenerse alineada con la implementación de MetaTrader.

## Gestión de riesgos

- **Stop-loss y take-profit.** Los objetivos de precio se definen en pips relativos al precio medio de entrada y se recalculan cada vez que cambia la posición.
- **Trailing stop.** Cuando está habilitado, el stop se arrastra detrás del precio cuando la ganancia abierta supera la distancia trailing.
- **Break-even.** Después de alcanzar el disparador configurado, el stop se mueve al precio de entrada más un desplazamiento opcional, protegiendo ganancias acumuladas.
- **Salida por Bollinger Band.** Las posiciones largas salen cuando el precio toca la banda inferior de un canal Bollinger de 20 períodos, mientras que las cortas salen al tocar la banda superior. Esto refleja la lógica de cierre basada en volatilidad del script MQL.
- **Confirmación MACD.** Las posiciones también se cierran ante un cruce de señal MACD (12, 26, 9) contra la dirección de la operación, reproduciendo la salida MACD mensual del experto original.
- **Interruptor de salida forzada.** El parámetro `EnableExitStrategy` permite que un operador liquide al instante toda posición abierta.

## Parámetros

| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `TradeVolume` | Volumen usado para cada paso de posición. | 0.1 |
| `CandleType` | Marco temporal de vela empleado para todos los indicadores. | Velas de 15 minutos |
| `FastMaPeriod` / `SlowMaPeriod` | Períodos de las medias móviles ponderadas lineales rápida y lenta. | 6 / 85 |
| `MomentumPeriod` | Retrospectiva de momentum usada en el bloque de confirmación. | 14 |
| `MomentumThreshold` | Desviación mínima desde 100 requerida para habilitar el trading. | 0.3 |
| `StopLossPips` / `TakeProfitPips` | Distancias de stop-loss y take-profit expresadas en pips. | 20 / 50 |
| `EnableTrailing` / `TrailingStopPips` | Interruptor y distancia para la función trailing stop. | true / 40 |
| `EnableBreakEven`, `BreakEvenTriggerPips`, `BreakEvenOffsetPips` | Interruptor de activación, disparador y desplazamiento de break-even en pips. | true, 30, 30 |
| `MaxPositions` | Número máximo de pasos de volumen permitidos. | 10 |
| `EnableExitStrategy` | Fuerza a la estrategia a dejar la posición plana cuando está habilitado. | false |

## Notas de conversión

- La implementación StockSharp se apoya en la canalización de alto nivel `SubscribeCandles().BindEx(...)` para procesar todos los indicadores simultáneamente y operar estrictamente sobre velas completadas.
- La conversión de pips usa el paso de precio del instrumento siempre que sea posible y recurre al valor del paso de precio cuando el bróker no expone precisión de pip, coincidiendo con el comportamiento adaptativo de la versión MetaTrader.
- La lógica de stop-loss, take-profit, trailing y break-even se gestiona internamente en lugar de usar órdenes de bróker, para mantener un comportamiento determinista durante backtests.
- Las llamadas de alerta, correo electrónico y notificación del experto MQL se eliminaron, ya que StockSharp proporciona sus propias capacidades de registro.

## Consejos de uso

1. Seleccione el instrumento deseado y ajuste `TradeVolume` y `MaxPositions` para adecuarlos al tamaño de la cuenta. Los valores predeterminados reproducen el escalado conservador usado en el EA.
2. Optimice `MomentumThreshold`, `StopLossPips` y `TrailingStopPips` con datos históricos si el mercado objetivo muestra características de volatilidad diferentes.
3. Al probar en múltiples símbolos, asegúrese de que el instrumento exponga un paso de precio correcto para que las distancias basadas en pips se conviertan con precisión.
4. Monitorice el log para la advertencia *"Unable to determine pip size from security settings"*. Si aparece, considere configurar el instrumento con el paso de precio correcto para evitar niveles de riesgo desajustados.
