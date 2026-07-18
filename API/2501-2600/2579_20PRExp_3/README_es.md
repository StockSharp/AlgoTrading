# Estrategia de Rompimiento 20PRExp-3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia 20PRExp-3 es un sistema de rompimiento que compara la sesión de trading actual con los extremos de precio del día anterior. Reconstruye el canal diario en cada vela de cinco minutos completada, confirma el momentum con una expansión del volumen de ticks de 30 minutos, y entra solo cuando el precio rompe más allá del máximo o mínimo de sesión actualizado. Una vez en el mercado, imita al experto original de MetaTrader 5 usando salidas de Parabolic SAR, stops trailing dinámicos y dimensionamiento fijo de riesgo basado en la distancia al stop de protección.

## Concepto
- **Canal diario**: Rastrear el máximo en ejecución, mínimo y punto medio del día de trading actual.
- **Confirmación de rompimiento**: Requerir que el precio cierre más allá del límite del canal con un filtro de rango diario mínimo (`GapPoints`).
- **Expansión de volumen**: Comparar las dos últimas velas de 30 minutos completadas y exigir al menos un aumento 1.5× en el volumen de ticks para evitar rompimientos finos.
- **Filtro de tiempo**: Permitir nuevas posiciones solo después de la hora de inicio de sesión configurada (`SessionStartHour`) para evitar el rango nocturno de baja liquidez.
- **Simetría de riesgo**: Las operaciones largas usan el mínimo diario como stop loss, las operaciones cortas usan el máximo diario. Los desplazamientos de take profit y trailing se miden en puntos de precio.

## Datos de mercado
- Velas de cinco minutos para la señal primaria y el cálculo del Parabolic SAR.
- Velas de treinta minutos para el filtro de ratio de volumen de ticks.
- Las estadísticas de máximo/mínimo diario se derivan al vuelo de los datos de cinco minutos, por lo que no se requiere suscripción diaria separada.

## Lógica de entrada
1. Esperar una vela de cinco minutos terminada después de la hora de inicio configurada.
2. Calcular el máximo/mínimo/punto medio del día actual y el ancho del canal.
3. Verificar que el ancho del canal exceda `GapPoints * PriceStep`.
4. Calcular el ratio de volumen de ticks = (último volumen de 30 minutos completado) / (volumen de 30 minutos anterior) y asegurar que sea mayor que 1.5.
5. **Configuración larga**: la vela cierra en o por encima del máximo diario actual → comprar.
6. **Configuración corta**: la vela cierra en o por debajo del mínimo diario actual → vender.
7. Omitir nuevas entradas mientras haya una posición activa (máximo un trade abierto).

## Gestión de salida
- **Stop inicial**: las operaciones largas usan el mínimo diario, las operaciones cortas usan el máximo diario capturado en la entrada.
- **Take profit**: opcional; colocado a `TakeProfitPoints * PriceStep` de la entrada en ambos lados del mercado.
- **Reversión Parabolic SAR**: cierra la posición si el valor de SAR cruza el cierre de la vela anterior (comportamiento del EA original).
- **Stop trailing**: se activa una vez que la ganancia excede `TrailingStopPoints * PriceStep` y se mueve al menos `TrailingStepPoints * PriceStep`.
- **Take trailing espejo**: siempre que se actualice el stop trailing, el nivel de take-profit se reposiciona simétricamente alrededor del cierre actual.

## Dimensionamiento de posición
- El volumen de posición se deriva de `RiskPercent`: la estrategia arriesga un porcentaje del valor actual del portafolio basado en la distancia entre entrada y stop.
- Si la valoración del portafolio no está disponible, el algoritmo recurre a `Volume + |Position|` y, como último recurso, opera un solo contrato.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| `CandleType` | Velas de 5 minutos | Marco temporal primario para señales y Parabolic SAR. |
| `VolumeCandleType` | Velas de 30 minutos | Marco temporal usado para evaluar la expansión del volumen de ticks. |
| `TakeProfitPoints` | 20 | Distancia al objetivo de ganancia expresada en puntos de precio. Establecer en 0 para deshabilitar. |
| `TrailingStopPoints` | 10 | Distancia en puntos para la activación del stop trailing. |
| `TrailingStepPoints` | 10 | Progreso adicional mínimo (en puntos) antes de que el stop trailing se mueva de nuevo. |
| `RiskPercent` | 5 | Porcentaje del capital del portafolio arriesgado por operación. |
| `GapPoints` | 50 | Ancho mínimo del canal diario en puntos requerido para habilitar un rompimiento. |
| `SessionStartHour` | 7 | Hora (0–23) después de la cual la estrategia puede abrir nuevas posiciones. |

## Notas
- Los parámetros de Parabolic SAR (paso 0.005, máx 0.01) coinciden con la estrategia MQL original.
- Los valores del punto medio diario se calculan para completitud y pueden graficarse como referencia visual si se desea.
- Dado que la expansión de volumen se evalúa en velas de 30 minutos completadas, la confirmación del rompimiento usa la última información cierre-a-cierre disponible, lo que es robusto tanto para pruebas históricas como para trading en vivo.
- Todos los comentarios en código están en inglés para alinearse con las directrices del repositorio.
