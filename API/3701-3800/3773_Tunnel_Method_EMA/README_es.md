# Estrategia del método del túnel EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia del Método del Túnel EMA** replica el asesor experto original del MetaTrader "Método del Túnel" en la API de alto nivel de StockSharp. Opera con velas horarias y compara tres promedios móviles exponenciales (EMA) basados en precios de cierre:

- **Fast EMA (12 periodos)** captura cambios de impulso inmediatos.
- **Medio EMA (144 períodos)** refleja el centro del "túnel" utilizado para validar señales cortas.
- **EMA lenta (169 períodos)** proporciona el filtro direccional a largo plazo para operaciones largas.

La estrategia mantiene posiciones mutuamente excluyentes (ya sean largas, cortas o planas) y gestiona dinámicamente el riesgo mediante controles explícitos de stop-loss, take-profit y trailing-stop.

## Lógica de señal
### Entradas largas
1. Espere a que se complete la vela (sin decisiones intrabar).
2. Detectar un cruce alcista donde el EMA rápido (12) se mueve desde abajo hacia arriba del EMA lento (169).
3. Confirme que no haya ninguna posición abierta actualmente y envíe una orden de compra de mercado para el volumen configurado.

### Entradas cortas
1. Espere a que se complete la vela.
2. Detectar un cruce bajista donde el EMA rápida (12) se mueve desde arriba hacia abajo del medio EMA (144).
3. Confirme que no haya ninguna posición abierta actualmente y envíe una orden de venta de mercado.

### Gestión de Puestos
- **Stop-Loss**: Cierra la operación cuando el precio se mueve contra la posición en `StopLossPoints` (convertido en precio absoluto usando el paso del precio del valor).
- **Take-Profit**: bloquea las ganancias una vez que el precio avanza `TakeProfitPoints` desde el precio de entrada.
- **Trailing Stop**: después de que la operación acumule al menos `TrailingTriggerPoints` de ganancias, la estrategia sigue el precio usando `TrailingStopPoints`. Para operaciones largas sigue el máximo más alto desde la entrada; para operaciones cortas, sigue el mínimo más bajo desde la entrada. Una reversión al nivel final cierra la posición.
- **Reinicio de estado**: Después de cada salida (manual o protectora), el estado de seguimiento interno se reinicia para evitar interferencias con operaciones posteriores.

## Parámetros predeterminados
| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `CandleType` | `TimeSpan.FromHours(1).TimeFrame()` | Velas horarias utilizadas para los cálculos de EMA. |
| `FastLength` | 12 | Duración del EMA rápido que reacciona a la acción reciente del precio. |
| `MediumLength` | 144 | Longitud del centro del túnel EMA para validación breve. |
| `SlowLength` | 169 | Longitud del límite del túnel EMA para validación larga. |
| `StopLossPoints` | 25 | Distancia de parada de protección en los puntos del instrumento. |
| `TakeProfitPoints` | 230 | Distancia objetivo de beneficio en puntos del instrumento. |
| `TrailingStopPoints` | 35 | Distancia mantenida por el trailing stop una vez activo. |
| `TrailingTriggerPoints` | 20 | Umbral de beneficio requerido antes de que comience el seguimiento. |

## Filtros y características
- **Categoría**: Crossover que sigue tendencias.
- **Instrumentos**: Funciona en cualquier instrumento que proporcione velas horarias y un incremento de precios confiable.
- **Dirección**: Opera tanto en largo como en corto, nunca manteniendo posiciones simultáneas.
- **Periodo**: velas de 1 hora de forma predeterminada (configurable a través de `CandleType`).
- **Controles de riesgo**: Stop-loss estricto, take-profit y trailing stop implementados dentro de la lógica de la estrategia.
- **Requisitos de datos**: Se basa exclusivamente en los precios de cierre de las velas; no se necesitan indicadores adicionales ni profundidad del mercado.

## Notas
- Todos los valores de los indicadores provienen de las implementaciones EMA de StockSharp para garantizar la coherencia con las directrices de alto nivel API.
- La estrategia ignora las velas inacabadas para evitar la doble contabilización de señales o actuar sobre datos parciales.
- Los ajustes de trailing stop respetan el `PriceStep` del valor a través de `ShrinkPrice`, manteniendo los niveles de salida alineados con los incrementos de tick válidos.
- Los parámetros predeterminados reflejan la configuración original de MQL, pero se pueden optimizar a través de las herramientas de optimización de parámetros de StockSharp.
