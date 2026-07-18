# Estrategia de FitFul 13
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
El asesor experto FitFul 13 trabaja alrededor de niveles de pivot semanales derivados de la semana de trading anterior. Espera a que la vela H1 actual (marco temporal predeterminado) reaccione a una de las bandas de pivot y confirma el movimiento con dos velas más antiguas de una serie de confirmación M15. Cuando la confirmación está presente, la estrategia abre una posición con niveles precalculados de stop-loss y take-profit derivados de la misma estructura de pivots. Un trailing stop protege las operaciones rentables una vez que el precio avanza lo suficiente.

## Lógica original
1. Calcular el precio típico y la estructura de pivot de la semana anterior: `PriceTypical`, `R1`, `S1`, niveles intermedios (`R0.5`, `S0.5`, `R1.5`, etc.) y las extensiones de segundo/tercer orden.
2. Observar la vela H1 más reciente. Si cerró alcista, buscar en el cuerpo de la vela precedente un cruce hacia arriba de uno de los niveles de pivot. Si ocurre tal cruce, preparar parámetros largos: stop por debajo del soporte relevante, take-profit por encima de la resistencia emparejada. Para cierres bajistas, la lógica espejo prepara parámetros cortos.
3. Si el cuerpo de la vela H1 no interactuó con ningún pivot, verificar dos velas M15 anteriores. Dos mínimos consecutivos perforando el mismo nivel confirman configuraciones largas, mientras que dos máximos cayendo a través de un nivel confirman cortos. Cada combinación se mapea a su propio par de stop/take.
4. Enviar una orden de mercado con el volumen neto configurado. El port de StockSharp trabaja con posiciones netas, por lo tanto la exposición opuesta se aplana antes de abrir la nueva operación. Los precios de stop-loss y take-profit se almacenan internamente y se aplican mediante salidas virtuales en nuevas velas.
5. Aplicar un trailing stop virtual: una vez que el beneficio abierto supere `TrailingStopPips + TrailingStepPips`, mover el stop a `close - TrailingStopPips` (largo) o `close + TrailingStopPips` (corto). El stop nunca retrocede y solo se ajusta si el precio avanza al menos el paso de trailing.
6. Ignorar nuevas señales si la posición neta absoluta ya equivale a `Volume × MaxPositions`.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
|--------|------|----------------|-------------|
| `CandleType` | `DataType` | H1 | Marco temporal principal usado para evaluar reacciones de pivot. |
| `ConfirmationCandleType` | `DataType` | M15 | Marco temporal inferior que proporciona la confirmación de dos barras. |
| `Volume` | `decimal` | 0.1 | Volumen de orden neto para cada entrada. |
| `MaxPositions` | `int` | 3 | Exposición neta máxima expresada como múltiplos de `Volume`. |
| `IndentPips` | `decimal` | 3 | Desplazamiento aplicado a los cálculos de stop-loss y take-profit basados en pivots. |
| `TrailingStopPips` | `decimal` | 150 | Distancia del trailing stop en pips. Establecer en cero para deshabilitar el trailing. |
| `TrailingStepPips` | `decimal` | 5 | Progreso de precio adicional mínimo (en pips) requerido antes de ajustar el trailing stop. |

## Notas sobre el port
- StockSharp gestiona una única posición neta. La capacidad de cobertura original se emula aplanando la exposición opuesta cuando se toma una nueva entrada.
- La lógica de stop-loss, take-profit y trailing se implementa virtualmente. La estrategia cierra posiciones en actualizaciones de velas cuando el precio cruza los niveles almacenados.
- Los pivots semanales se recalculan cada vez que se recibe una nueva vela semanal. La confirmación predeterminada usa H1/M15, pero ambos marcos temporales pueden ajustarse mediante parámetros.
- Todos los comentarios en el código fuente están escritos en inglés según lo requerido por las directrices de conversión.
