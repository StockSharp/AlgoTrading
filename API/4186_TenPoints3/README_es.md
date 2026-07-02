# Estrategia Diez Puntos 3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen
- Convierte el asesor experto MetaTrader 4 **10p3v004 ("10points 3")** en el marco estratégico de alto nivel StockSharp.
- Recrea la lógica de entrada de la cuadrícula basada en pendientes MACD junto con escalado de martingala, protección de seguimiento y salidas basadas en equidad.
- Proporciona documentación exhaustiva de cada parámetro para que el comportamiento del EA original pueda reproducirse o ajustarse de forma segura.

## Lógica de trading
1. **Detección de señal.** En cada vela completada del período configurado, la estrategia calcula un MACD con longitudes de señal rápida, lenta y definidas por el usuario. Cuando el valor principal MACD aumenta en comparación con la barra anterior, el sistema prepara una cuadrícula larga; cuando cae se prepara una rejilla corta. La bandera `ReverseSignals` invierte esta interpretación.
2. **Entradas de cuadrícula.** Solo puede haber una cuadrícula direccional activa a la vez. La primera orden se realiza inmediatamente después de una señal. Se agregan pedidos adicionales si:
   - La dirección de la cuadrícula activa coincide con la señal actual, y
   - El precio se ha movido al menos `GridSpacingPoints * PriceStep` desde el llenado más reciente en la dirección de promedio favorable, y
   - El número de operaciones de red abierta no ha llegado a `MaxTrades`.
El tamaño del pedido se multiplica por `2^n` para cuadrículas pequeñas (hasta 12 entradas) o por `1.5^n` para cuadrículas más grandes, reproduciendo la lógica de martingala del código fuente. El tamaño final se redondea al paso de volumen del instrumento y está limitado tanto por los límites de seguridad como por el techo de seguridad `MaxVolumeCap`.
3. **Administración de dinero.** Cuando `UseMoneyManagement` está habilitado, el tamaño del lote base se deriva del valor actual de la cartera y `RiskPerTenThousand`. El EA original usaba reglas separadas para cuentas estándar y mini; esta conversión mantiene el mismo comportamiento a través del parámetro `IsStandardAccount`. Si la configuración está deshabilitada, se utiliza el `BaseVolume` fijo.
4. **Reglas de salida.**
   - La **parada inicial** opcional cierra toda la cuadrícula si la posición agregada se mueve contra ella en `InitialStopPoints`.
   - El **obtener ganancias** opcional cierra la cuadrícula una vez que el precio alcanza `TakeProfitPoints` a favor de la posición neta.
   - El **trailing stop** opcional comienza a seguir el precio después de que se mueve `(TrailingStopPoints + GridSpacingPoints)` desde el precio de entrada promedio y mantiene un buffer de seguimiento de `TrailingStopPoints`.
   - La **protección de capital** opcional monitorea las ganancias no realizadas medidas en puntos multiplicados por el volumen. Cuando hay `OrdersToProtect` o más posiciones abiertas y la ganancia alcanza `SecureProfit`, la estrategia sale inmediatamente.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `CandleType` | Periodo de tiempo principal utilizado para los cálculos de MACD y el procesamiento de pedidos. | velas de 30 minutos |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | MACD configuración idéntica al indicador MT4 (26/14/9 por defecto). | 14 / 26 / 9 |
| `BaseVolume` | El tamaño del lote inicial se utiliza cuando no existe una posición en la cuadrícula y la administración del dinero está deshabilitada. | 0,01 |
| `GridSpacingPoints` | Distancia mínima entre entradas consecutivas de la cuadrícula, expresada en pasos de precio. | 15 |
| `TakeProfitPoints` | Distancia desde la entrada promedio para desencadenar una toma de ganancias total. Establezca en `0` para desactivar. | 40 |
| `InitialStopPoints` | Distancia máxima adversa tolerada antes de aplanar la rejilla. Establezca en `0` para desactivar. | 0 |
| `TrailingStopPoints` | Tamaño del búfer final. El recorrido se activa después de que el precio haya avanzado `GridSpacingPoints + TrailingStopPoints`. | 20 |
| `MaxTrades` | Número máximo de pedidos promedio por dirección. | 9 |
| `OrdersToProtect` | Número mínimo de operaciones abiertas requeridas antes de que se evalúe la verificación de protección de acciones. | 3 |
| `SecureProfit` | Objetivo de beneficio no realizado (puntos × volumen) que desencadena la salida de la protección del capital. | 8 |
| `AccountProtectionEnabled` | Habilita o deshabilita el bloque de protección de acciones. | `true` |
| `ReverseSignals` | Invierte la interpretación de la pendiente MACD (útil para pruebas reflejadas). | `false` |
| `UseMoneyManagement` | Habilita el cálculo de volumen dinámico usando `RiskPerTenThousand`. | `false` |
| `RiskPerTenThousand` | Monto de riesgo por 10,000 unidades de saldo utilizadas cuando la administración del dinero está activa. | 12 |
| `IsStandardAccount` | Replica las reglas de redondeo de lotes originales (`true` = lotes estándar, `false` = mini lotes). | `true` |
| `MaxVolumeCap` | Se aplica una tapa dura después del escalado de martingala para mantener el tamaño de la posición bajo control. | 100 |

## Notas de conversión
- El experto MQL mantuvo paradas separadas a nivel de boleto. En StockSharp la cuadrícula se gestiona como una única posición agregada. Por lo tanto, los niveles de seguimiento y de protección se recalculan a partir del precio de entrada promedio ponderado por volumen.
- El EA se basó en el valor del corredor para convertir las ganancias en moneda. Aquí el umbral de protección del capital se mide en puntos multiplicados por el volumen, reflejando la comparación basada en pips de la fuente.
- `AccountFreeMarginCheck` y otras validaciones MT4 específicas de la cuenta no tienen un equivalente directo de StockSharp. En cambio, la estrategia respeta los límites de volumen del instrumento y el `MaxVolumeCap` opcional.
- Los comentarios de pedidos, los números mágicos y las anotaciones gráficas de MT4 no se reproducen porque no tienen una contraparte StockSharp.

## Uso
1. Agregue la estrategia a su proyecto, configure `Security` y `Portfolio` como de costumbre para las estrategias StockSharp.
2. Ajuste `CandleType` para que coincida con el período de tiempo que debe analizarse (la versión MT4 funcionó en el período de tiempo actual del gráfico).
3. Ajuste los parámetros de riesgo: mantenga el `BaseVolume` fijo o habilite `UseMoneyManagement` con las opciones `RiskPerTenThousand` y `IsStandardAccount` apropiadas.
4. Decida qué capas de protección habilitar (stop inicial, toma de ganancias, stop dinámico, protección de acciones) y establezca los umbrales para adaptarse a la volatilidad del instrumento.
5. Iniciar la estrategia; los ayudantes de gráficos integrados mostrarán velas, valores MACD y operaciones ejecutadas.

## Ideas de desarrollo adicionales
- Integre la lógica de espaciado adaptable (por ejemplo, usando ATR) en lugar del fijo `GridSpacingPoints`.
- Exponga parámetros finales separados para cuadrículas largas y cortas o permita cuadrículas asimétricas.
- Combine la pendiente MACD con filtros de tendencia (promedios móviles, confirmación de período de tiempo más alto) para reducir la cantidad de cuadrículas de contratendencia.

> **Nota:** No se proporciona ninguna implementación de Python para esta estrategia, que coincide con la solicitud y la estructura actual del proyecto.
