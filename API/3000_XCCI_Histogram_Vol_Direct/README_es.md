# Estrategia XCCI Histogram Vol Direct
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia XCCI Histogram Vol Direct** es una conversión del experto MQL5 `Exp_XCCI_Histogram_Vol_Direct`. El sistema multiplica el Commodity Channel Index (CCI) por el volumen, suaviza ambas series con una media móvil configurable y evalúa la pendiente del oscilador suavizado. Cuando el color direccional del histograma cambia, la estrategia cierra posiciones en contra del movimiento y abre nuevas operaciones en la dirección emergente. La lógica opera únicamente sobre velas finalizadas, por lo que se comporta de manera determinista tanto en datos históricos como en tiempo real.

El asesor original utilizaba una biblioteca de suavizado propietaria con múltiples algoritmos, bandas de umbral basadas en volumen y ejecución de órdenes con desplazamiento temporal. El puerto para StockSharp conserva los parámetros configurables, aproxima las opciones de suavizado con indicadores disponibles e implementa la misma secuencia de apertura/cierre mediante la API de alto nivel.

## Régimen de mercado y ventaja
- Diseñado para mercados donde la expansión del volumen acompaña las ráfagas de momentum.
- Prefiere marcos temporales con oscilaciones claras (predeterminado: velas de 2 horas), pero puede ajustarse desde intradía hasta horizontes de swing.
- Las señales reaccionan a un cambio en la pendiente del CCI*volumen suavizado; por tanto, se comporta como un detector de reversión de momentum.

## Indicadores y flujo de procesamiento
1. **Commodity Channel Index (CCI)** – calculado sobre el tipo de vela seleccionado con período `CciPeriod`.
2. **Fuente de volumen** – `Tick` o `Real` (ambos mapeados al volumen de vela porque los recuentos de ticks no están disponibles en las velas de StockSharp).
3. **Oscilador ponderado** – multiplicar el CCI por el flujo de volumen elegido.
4. **Suavizado** – aplicar la familia de medias móviles seleccionada tanto al oscilador ponderado como al volumen bruto usando la longitud `SmoothingLength`.
   - `Sma` → SimpleMovingAverage
   - `Ema` → ExponentialMovingAverage
   - `Smma` → SmoothedMovingAverage
   - `Lwma` → WeightedMovingAverage
   - `Jjma` → JurikMovingAverage
   - `Jurx` → ZeroLagExponentialMovingAverage
   - `Parabolic` → ArnaudLegouxMovingAverage (el parámetro de fase se mapea al desplazamiento ALMA)
   - `T3` → TripleExponentialMovingAverage
   - `Vidya` → ExponentialMovingAverage (mejor aproximación disponible)
   - `Ama` → KaufmanAdaptiveMovingAverage
5. **Color direccional** – comparar el último valor suavizado del oscilador con el anterior. Los valores crecientes se colorean `0` (alcista), los valores decrecientes `1` (bajista), y los valores iguales heredan el color previo, igual que el búfer del indicador original.
6. **Memoria de señales** – almacenar los colores recientes para que la estrategia pueda inspeccionar la barra especificada por `SignalBar` y la barra anterior.

## Reglas de operación
### Gestión de posiciones largas
- **Entrada**: Si el color de la barra de señal es `1` (bajista) pero la barra anterior fue `0` (alcista), abrir una posición larga siempre que `AllowLongEntries = true` y la posición neta actual no sea ya larga. El tamaño de la orden es `Volume + |Position|`, por lo que cualquier exposición corta se cierra primero.
- **Salida**: Cuando la barra anterior a la señal es alcista (`0`) y `AllowShortExits = true`, cerrar cualquier posición corta abierta para evitar luchar contra el nuevo impulso alcista.

### Gestión de posiciones cortas
- **Entrada**: Si el color de la barra de señal pasa a `0` tras un `1` previo, abrir una posición corta cuando `AllowShortEntries = true` y la cuenta no esté ya neta corta. El tamaño de la orden refleja la lógica larga.
- **Salida**: Cuando la barra anterior a la señal es bajista (`1`) y `AllowLongExits = true`, cerrar la exposición larga.

### Controles de riesgo
- `StopLossPoints` y `TakeProfitPoints` se traducen en desplazamientos de precio usando el `PriceStep` del instrumento y se aplican mediante `StartProtection`.
- Las órdenes de protección se activan para cada operación; establezca ambos valores en `0` para deshabilitar un tramo individual.

## Referencia de parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|----------------|
| `CciPeriod` | Longitud del Commodity Channel Index. | `14` |
| `Smoothing` | Familia de medias móviles utilizada para suavizar el oscilador y el volumen. | `T3` |
| `SmoothingLength` | Período de los filtros de suavizado. | `12` |
| `SmoothingPhase` | Valor de fase/desplazamiento mapeado al desplazamiento ALMA; conservado por compatibilidad. | `15` |
| `HighLevel2`, `HighLevel1`, `LowLevel1`, `LowLevel2` | Multiplicadores de umbral preservados del indicador (útiles para diagnóstico/visualización). | `100`, `80`, `-80`, `-100` |
| `SignalBar` | Índice de retroceso de la barra que define la señal (0 = última vela cerrada). | `1` |
| `AllowLongEntries` / `AllowShortEntries` | Habilitar o deshabilitar la apertura de operaciones en una dirección. | `true` |
| `AllowLongExits` / `AllowShortExits` | Habilitar o deshabilitar el cierre de operaciones en una dirección. | `true` |
| `StopLossPoints` | Distancia del stop-loss en puntos de precio. | `1000` |
| `TakeProfitPoints` | Distancia del take-profit en puntos de precio. | `2000` |
| `VolumeSource` | Flujo de volumen (`Tick` o `Real`). Ambos usan el volumen de vela en este puerto. | `Tick` |
| `CandleType` | Marco temporal para el análisis. | `2h` |

## Flujo de trabajo de procesamiento de velas
1. Esperar una vela finalizada del tipo configurado.
2. Calcular el valor del CCI y multiplicarlo por el flujo de volumen seleccionado.
3. Introducir el CCI ponderado y el volumen bruto en los filtros de suavizado.
4. Una vez que ambos suavizadores estén formados, determinar el nuevo color y actualizar el búfer de historial.
5. Inspeccionar el color en `SignalBar` y `SignalBar+1` para decidir si cerrar posiciones contrarias y/o abrir una nueva operación.
6. Aplicar gestión de riesgos mediante el stop-loss y take-profit preconfigurados.

## Notas de uso
- La `Strategy.Volume` base debe establecerse en un valor positivo; define el tamaño de cada entrada.
- Dado que las velas de StockSharp no exponen recuentos de ticks, tanto el modo `Tick` como el `Real` de volumen usan `candle.TotalVolume`. Si se requieren datos a nivel de tick, alimente la estrategia con velas personalizadas que codifiquen el volumen de ticks en el campo `TotalVolume`.
- La fase de suavizado afecta únicamente a ALMA. Para otros filtros se ignora, reflejando el comportamiento del indicador MQL donde ciertos modos ignoran la entrada de fase.
- Los multiplicadores de umbral (`HighLevel*` y `LowLevel*`) se conservan por completitud. Pueden visualizarse trazando el volumen suavizado y aplicando los multiplicadores externamente si se desea.

## Limitaciones y diferencias respecto a la versión MQL5
- StockSharp actualmente carece de implementaciones directas de VIDYA y Parabolic MA; se utilizan EMA y ALMA como sustitutos más cercanos. Esto mantiene las características de respuesta similares pero no idénticas a la biblioteca personalizada original.
- La ejecución de órdenes ocurre inmediatamente al cierre de la vela de señal. El experto MQL programaba operaciones al inicio del siguiente período mediante `TimeShiftSec`; este comportamiento es funcionalmente equivalente cuando el broker ejecuta órdenes de mercado casi instantáneamente.
- El volumen de ticks se aproxima por el volumen total negociado porque los recuentos individuales de ticks no se exponen en los mensajes estándar de velas.

## Primeros pasos
1. Vincule la estrategia al `Security` deseado y establezca `Volume` al número de lotes/contratos a operar por señal.
2. Elija el marco temporal de velas mediante `CandleType` (predeterminado: marco temporal de 2 horas).
3. Ajuste los parámetros de suavizado y riesgo para adaptarlos al perfil de volatilidad del mercado objetivo.
4. Ejecute primero en modo papel, revise el oscilador suavizado graficado y ajuste `SignalBar` si las señales llegan demasiado pronto o tarde.

## Ideas de optimización
- Optimizar `SmoothingLength` junto con `CciPeriod` para alinear la capacidad de respuesta con el activo objetivo.
- Realizar pruebas de estrés de `SignalBar` alrededor de `0` y `1` para una reacción más rápida/lenta.
- Considerar ampliar o reducir `StopLossPoints` / `TakeProfitPoints` para adaptarse al ATR del instrumento.
- Ejecutar la estrategia en múltiples marcos temporales y filtrar operaciones por la dirección de tendencia del marco temporal superior si se necesita confirmación adicional.

## Lista de verificación de seguridad
- Confirmar que `Security.PriceStep` y `Volume` coincidan con las especificaciones del contrato del instrumento antes de la ejecución en vivo.
- Monitorear el deslizamiento y ajustar los controles de riesgo externos si el mercado elegido es ilíquido.
- Revisar regularmente los registros de operaciones para asegurar que los filtros de dirección (`Allow*`) se alineen con la exposición prevista.
