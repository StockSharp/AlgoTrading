# Estrategia MACD con Filtro de Línea Cero y Toma de Ganancias
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia replica el experto original de MetaTrader 5 "Robot_MACD" que opera cruces de la línea de señal del MACD con filtros adicionales de línea cero. Opera sobre un único instrumento y busca reversiones de momentum confirmadas por la posición de la línea MACD relativa a cero. Se adjunta una toma de ganancias a distancia fija en cada orden, reproduciendo el objetivo de ganancia basado en puntos de la implementación original.

## Datos e Indicadores
- **Datos primarios**: suscripción a una sola vela (marco temporal de 5 minutos por defecto). El marco temporal puede cambiarse con el parámetro `CandleType` para adaptarse al mercado negociado.
- **Indicadores**:
  - `MovingAverageConvergenceDivergenceSignal` (MACD + señal + histograma). Los valores predeterminados son EMAs de 12/26 con una línea de señal de 9 períodos, coincidiendo con los parámetros MQL.

## Lógica de Operación
1. Esperar a que el cálculo del MACD proporcione los valores actuales y anteriores de las líneas MACD y de señal.
2. Identificar cruces alcistas y bajistas:
   - **Cruce alcista**: MACD anterior ≤ señal anterior **y** MACD actual > señal actual.
   - **Cruce bajista**: MACD anterior ≥ señal anterior **y** MACD actual < señal actual.
3. **Gestión de posiciones**:
   - Cerrar una posición larga cuando aparece un cruce bajista.
   - Cerrar una posición corta cuando aparece un cruce alcista.
4. **Criterios de entrada** (solo cuando no hay posición abierta y hay capital suficiente):
   - Entrar largo en un cruce alcista **mientras tanto el MACD como la señal permanezcan por debajo de cero**.
   - Entrar corto en un cruce bajista **mientras tanto el MACD como la señal permanezcan por encima de cero**.
5. Adjuntar una toma de ganancias fija en el momento del registro de la orden llamando a `StartProtection` con una distancia absoluta medida en puntos de precio. La distancia equivale al valor de punto configurado multiplicado por el paso de precio del instrumento.

## Gestión de Riesgos
- Cada orden tiene una toma de ganancias adjunta definida por `TakeProfitPoints`. No hay stop-loss en la lógica base, manteniendo la paridad con el EA fuente.
- La estrategia verifica si el valor del portafolio es al menos `MinimumCapitalPerVolume * VolumePerTrade` antes de colocar una nueva orden. Esto emula la protección de margen libre (`FreeMargin() < 1000 * Lots`) de la versión MQL.

## Parámetros
| Parámetro | Descripción | Valor predeterminado |
|-----------|-------------|----------------------|
| `MacdFast` | Período EMA rápido para MACD. | 12 |
| `MacdSlow` | Período EMA lento para MACD. | 26 |
| `MacdSignal` | Período de suavizado de la línea de señal. | 9 |
| `TakeProfitPoints` | Distancia de toma de ganancias expresada en puntos de precio. | 300 |
| `VolumePerTrade` | Volumen de negociación (lotes) utilizado para cada entrada. | 1 |
| `MinimumCapitalPerVolume` | Valor mínimo del portafolio requerido por lote negociado. | 1000 |
| `CandleType` | Tipo de vela (marco temporal) utilizado para alimentar el indicador MACD. | Velas de 5 minutos |

## Notas de Implementación
- Las órdenes se ejecutan con `BuyMarket`/`SellMarket`, idéntico al EA que usaba órdenes de mercado vía `CTrade`.
- Los filtros de línea cero evitan entrar en operaciones en la mitad opuesta del histograma MACD, tal como en el script MQL.
- La verificación del valor del portafolio depende de `Portfolio.CurrentValue`. Si el entorno de trading no suministra este valor, la protección pasa automáticamente, lo que mantiene la estrategia utilizable en cuentas simuladas.
- La sección de dibujo en el gráfico representa velas, el indicador MACD y marcadores de operaciones cuando hay un área de gráfico disponible en la aplicación host.
