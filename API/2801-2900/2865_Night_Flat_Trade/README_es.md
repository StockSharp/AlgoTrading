# Estrategia de Trading Nocturno en Rango Plano
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia de Trading Nocturno en Rango Plano reproduce el asesor experto MQL5 clásico que busca rangos nocturnos ajustados en velas H1 de EURUSD. Se enfoca en la hora que rodea el cambio del día de trading, esperando que el precio regrese hacia los bordes de un canal de consolidación estrecho y apostando a la continuación de un rompimiento. La versión StockSharp mantiene las ideas originales intactas mientras depende de suscripciones de velas de alto nivel, vínculos de indicadores y objetos de parámetros para mejor configurabilidad.

## Descripción general

- **Mercado y marco temporal**: Diseñado para EURUSD en el marco temporal H1, pero puede usarse cualquier instrumento con un paso de precio claramente definido.
- **Ventana de sesión**: Las entradas están permitidas solo durante una ventana de dos horas que comienza en el `OpenHour` configurado y termina en `OpenHour + 1` (hora de la bolsa).
- **Filtro de rango**: El tramo alto-bajo de las últimas tres velas completadas debe permanecer entre `DiffMinPips` y `DiffMaxPips` (convertidos a unidades de precio).
- **Sesgo**: Solo largo o solo corto dependiendo de dónde se sitúa el último cierre dentro del rango que califica.

## Lógica de trading

1. **Calcular los límites del rango**
   - La estrategia vincula a los indicadores incorporados `Highest` y `Lowest` (longitud = 3) para obtener el máximo más alto y el mínimo más bajo a lo largo de las últimas tres velas.
   - La distancia entre esas fronteras es el rango de trabajo usado para todas las comprobaciones posteriores.

2. **Condiciones de entrada**
   - **Configuración larga**: Durante la sesión activa, si el precio de cierre está por encima del mínimo del rango pero aún dentro del cuarto inferior (`lowest + range/4`), la estrategia abre una posición larga con un stop protector inicial en `lowest - range/3`.
   - **Configuración corta**: Simétricamente, si el cierre está por debajo del máximo del rango pero aún dentro del cuarto superior (`highest - range/4`), se abre una posición corta con un stop en `highest + range/3`.

3. **Gestión de salidas**
   - **Stop-Loss**: Los stops se simulan internamente y activan una salida de mercado cuando la siguiente vela viola el umbral almacenado.
   - **Take-Profit**: Cuando `TakeProfitPips > 0`, se crea un nivel adicional fijo de take-profit (en pips) relativo al precio de entrada.
   - **Trailing Stop**: Cuando tanto `TrailingStopPips` como `TrailingStepPips` son positivos, el stop se ajusta solo después de que el precio avanza `TrailingStop + TrailingStep` pips a favor de la operación. Los ajustes posteriores requieren un progreso adicional de `TrailingStepPips` para reflejar el comportamiento de trailing escalonado original.

4. **Control de re-entrada**
   - El algoritmo siempre espera a que la posición actual esté completamente cerrada antes de buscar una nueva señal, manteniendo el sistema plano entre operaciones como en el asesor experto de referencia.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `CandleType` | Serie de velas a suscribir (por defecto H1). | Velas de 1 hora |
| `TakeProfitPips` | Distancia de take-profit opcional en pips. | 50 |
| `TrailingStopPips` | Distancia entre precio y trailing stop en pips (0 desactiva trailing). | 15 |
| `TrailingStepPips` | Pips adicionales requeridos antes de cada actualización del trailing stop. | 5 |
| `DiffMinPips` | Rango mínimo permitido de tres velas (pips). | 18 |
| `DiffMaxPips` | Rango máximo permitido de tres velas (pips). | 28 |
| `OpenHour` | Hora de inicio de la sesión en hora de la bolsa (entradas permitidas hasta `OpenHour + 1`). | 0 |

## Indicadores

- `Highest(Length = 3)` para monitorear la parte superior del rango reciente.
- `Lowest(Length = 3)` para monitorear la parte inferior del rango reciente.

## Notas de implementación

- La conversión de pips se adapta automáticamente a instrumentos con 3 o 5 decimales multiplicando el paso de precio reportado por 10, exactamente como la implementación original de MQ5.
- Dado que StockSharp opera en velas completadas en este ejemplo, las condiciones de entrada intra-vela se aproximan usando el precio de cierre. Esto mantiene la lógica determinista mientras permanece fiel al intento del código fuente.
- Todos los parámetros de riesgo están expuestos mediante objetos `StrategyParam<T>`, haciéndolos visibles en la UI y listos para optimización o experimentos por lotes.
