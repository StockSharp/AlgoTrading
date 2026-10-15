# Estrategia SilverTrend V3 JTPO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
SilverTrend V3 es una estrategia de seguimiento de tendencias traducida de la implementación original MetaTrader 4. Evalúa el indicador SilverTrend junto con el filtro estadístico J_TPO para identificar nuevos cambios direccionales. La estrategia opera con un solo instrumento a la vez y aplica una regla fija los viernes por la noche para evitar mantener el riesgo durante el fin de semana.

## Lógica de trading
1. **Procesamiento de indicadores**
   - La estrategia mantiene un búfer continuo de velas recientes y recalcula la dirección de SilverTrend en cada barra completa.
   - SilverTrend utiliza una ventana de 9 barras y un factor de riesgo de 3 para determinar los límites del canal adaptativo. Si el precio de cierre supera el límite superior, la señal se vuelve alcista; cruzar por debajo del límite inferior convierte la señal a bajista.
   - El cálculo J_TPO (longitud 14) mide la asimetría de la distribución de precios. Solo los valores positivos de J_TPO confirman entradas largas, mientras que se requieren lecturas negativas para entradas cortas.
2. **Condiciones de entrada**
   - Se abre una operación larga cuando la señal de SilverTrend cambia de bajista a alcista y J_TPO está por encima de cero.
   - Se abre una operación corta cuando la señal de SilverTrend cambia de alcista a bajista y J_TPO está por debajo de cero.
   - Las nuevas posiciones se bloquean los viernes una vez que la hora del mercado excede el límite configurado.
3. **Gestión de salida**
   - Las señales opuestas de SilverTrend cierran las operaciones abiertas inmediatamente.
   - Los niveles opcionales de stop loss inicial y toma de ganancias se colocan a distancias fijas (expresadas en puntos).
   - Un trailing stop opcional sigue el precio una vez que supera el buffer de ganancias configurado.

## Parámetros
| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `Volume` | Tamaño del pedido en lotes. | `1` |
| `TrailingStopPoints` | Distancia del trailing stop en puntos de precio. `0` desactiva el seguimiento. | `0` |
| `TakeProfitPoints` | Tome la distancia de ganancias en puntos de precio. `0` desactiva la toma de ganancias. | `0` |
| `InitialStopPoints` | Distancia inicial de stop loss en puntos de precio. `0` desactiva la parada de protección. | `0` |
| `FridayCutoffHour` | Hora (hora de cambio) después de la cual no se permiten nuevas operaciones el viernes. | `16` |
| `CandleType` | Tipo de vela o período de tiempo utilizado para el análisis. | `1h` velas |

## Notas adicionales
- Solo hay una posición abierta en cualquier momento, lo que coincide con el comportamiento de operación única del asesor experto original.
- La implementación utiliza API de alto nivel de StockSharp, por lo que la estrategia se suscribe a velas y realiza lógica solo en barras terminadas.
- Los stop dinámicos y fijos se gestionan internamente y cerrarán la posición al precio de mercado una vez activados.
