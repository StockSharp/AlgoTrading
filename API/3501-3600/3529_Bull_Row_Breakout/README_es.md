# Estrategia de ruptura de Bull Row
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Bull Row Breakout es una conversión de C# del asesor experto MetaTrader 5 "BULL row full EA". El robot original fue construido con un constructor de bloques y combina patrones de acción del precio con confirmación de impulso. El puerto StockSharp reproduce la misma lógica en un único período de tiempo configurable y mantiene los comentarios comerciales en inglés según sea necesario.

La estrategia abre posiciones **solo largas** después de que una secuencia de velas bajistas sea seguida por un impulso alcista y una ruptura por encima de los máximos recientes. Stochastic Los filtros del oscilador controlan la fuerza del impulso, mientras que el stop loss dinámico y los niveles objetivo recrean la configuración de riesgo de la versión MQL.

## Lógica de señal
1. Espere a que se cierre una nueva vela (ejecución "una vez por barra").
2. Verifique que no haya ninguna posición larga abierta actualmente.
3. Detectar una fila bajista:
   - `BearRowSize` velas consecutivas que comienzan en `BearShift` barras hacia atrás deben ser bajistas.
   - El cuerpo de cada vela debe tener al menos `BearMinBody` pasos de precio.
   - La progresión del cuerpo debe satisfacer el `BearRowMode` seleccionado (normal/más grande/más pequeño).
4. Detectar una fila alcista:
   - `BullRowSize` velas consecutivas que comienzan en `BullShift` barras hacia atrás deben ser alcistas.
   - El cuerpo de cada vela debe tener al menos `BullMinBody` pasos de precio.
   - La progresión corporal debe satisfacer `BullRowMode`.
5. Confirmación de ruptura: el cierre de la última vela terminada debe ser mayor que el máximo más alto registrado desde la barra 2 hasta hace `BreakoutLookback` barras.
6. Stochastic confirmación:
   - El %K actual (`StochasticKPeriod`) debe estar por encima de %D (`StochasticDPeriod`).
   - Los últimos valores de `StochasticRangePeriod` %K deben permanecer entre `StochasticLowerLevel` y `StochasticUpperLevel`.
7. Gestión de riesgos:
   - El precio stop es el mínimo más bajo entre las últimas `StopLossLookback` velas (comenzando desde la última barra cerrada).
   - La toma de ganancias se coloca a una distancia igual al `TakeProfitPercent` por ciento de la distancia de parada.
   - El stop y el objetivo se controlan en cada vela cerrada; Si se alcanza cualquiera de los niveles intrabar, la posición se cierra en el mercado en la siguiente actualización.

## Parámetros
| Parámetro | Descripción |
| --- | --- |
| `Volume` | Volumen comercial fijo utilizado para cada entrada. |
| `CandleTimeFrame` | Plazo de las velas procesadas. |
| `StopLossLookback` | Número de barras utilizadas para calcular el precio stop dinámico. |
| `TakeProfitPercent` | Distancia de recompensa expresada como porcentaje de la distancia de parada. |
| `BearRowSize`, `BearMinBody`, `BearRowMode`, `BearShift` | Configuración de la fila bajista que precede a la ruptura. |
| `BullRowSize`, `BullMinBody`, `BullRowMode`, `BullShift` | Configuración de la fila alcista que precede inmediatamente a la señal. |
| `BreakoutLookback` | Longitud del máximo móvil utilizado para la confirmación de ruptura. |
| `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlowing` | Stochastic Configuración del oscilador. |
| `StochasticRangePeriod` | Número de valores históricos Stochastic que deben permanecer dentro de los límites. |
| `StochasticUpperLevel`, `StochasticLowerLevel` | Límites del canal del oscilador aplicados a %K. |

Todos los tamaños de cuerpo se expresan en incrementos de precios para reflejar el asistente `toDigits` del código original. Cuando el instrumento no proporciona un escalón de precio, se utiliza un valor predeterminado de 1.

## Diferencias con la versión MQL
- El proyecto MT5 permitió períodos de tiempo separados para las entradas del bloque. El puerto StockSharp opera en un período de tiempo definido por `CandleTimeFrame`, coincidiendo con el uso común del EA original (todos los bloques en el período de tiempo del gráfico).
- Las paradas virtuales y el manejo de órdenes pendientes de la biblioteca de bloques genérica no son necesarios y, por lo tanto, se omiten.
- Los niveles protectores de stop-loss y take-profit se emulan monitoreando las velas y cerrando la posición con `SellMarket` una vez que se supera un nivel.
- Las decoraciones de registros y gráficos del entorno MQL no se replican.

## Consejos de uso
- Optimice los tamaños de fila y los cambios para el instrumento negociado. Los valores predeterminados imitan el ajuste preestablecido original (tres velas bajistas que comienzan tres barras hacia atrás seguidas de dos velas alcistas que comienzan una barra hacia atrás).
- Ajuste `StochasticLowerLevel` y `StochasticUpperLevel` para ajustar qué tan restrictivo debe ser el filtro del oscilador.
- Debido a que el stop se basa en mínimos recientes, los instrumentos con grandes brechas pueden requerir ampliar la perspectiva retrospectiva o agregar filtros adicionales.
