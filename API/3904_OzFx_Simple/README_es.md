# Estrategia simple de OzFx
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Conversión del MetaTrader 4 asesor experto **OzFx** (carpeta `MQL/7994`) al API de alto nivel de StockSharp.
- Utiliza el oscilador acelerador/desacelerador (AC) junto con la línea %K del oscilador estocástico para detectar inversiones de impulso alrededor de la línea cero.
- Replica el comportamiento del experto de acumular cinco órdenes de mercado con toma de ganancias escalonadas y protección de equilibrio después de alcanzar el primer objetivo.

## Lógica de trading
1. Construya el Oscilador Impresionante (5/34) y reste su SMA de 5 períodos para obtener el valor del Oscilador Acelerador de la vela completada anterior y actual.
2. Suscríbase al oscilador estocástico (%K longitud = `StochasticLength`, suavizado 3/3) y lea la línea principal al cerrar la vela.
3. **La configuración larga** requiere:
   - `%K` por encima del nivel medio configurado (predeterminado 50).
   - Valor AC actual positivo y superior al anterior.
   - El valor de CA anterior aún está por debajo de cero (el impulso cruza la línea de base).
4. **Configuración corta** refleja las reglas en la dirección opuesta.
5. Cuando aparece una señal en una nueva barra, la estrategia abre cinco órdenes de mercado iguales:
   - Las capas 1 a 4 reciben tomas de ganancias espaciadas por `TakeProfitPips` múltiplos.
   - La capa 5 no tiene objetivo de ganancias y sigue a la zaga del movimiento.
6. Si aparece la configuración opuesta mientras una pila está abierta, las órdenes restantes se cierran en el mercado, manteniendo la estrategia plana antes de nuevas entradas.

## Gestión de Puestos
- Cada capa comparte la misma distancia de stop-loss definida por `StopLossPips`.
- Después de que se ejecuta la primera toma de ganancias, las órdenes restantes ajustan sus límites al precio de equilibrio (de entrada), coincidiendo con la lógica "modok" original.
- Las salidas protectoras se ejecutan cuando los extremos de las velas perforan el stop almacenado o los niveles objetivo; Las órdenes pendientes del corredor no se utilizan.
- La estrategia permite solo una dirección a la vez y espera a que se cierren todas las órdenes antes de restablecer las banderas del bloque de entrada.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `OrderVolume` | Tamaño de lote para cada una de las cinco órdenes de mercado. | `0.1` |
| `StopLossPips` | Distancia entre la entrada y el stop loss, expresada en pips. | `100` |
| `TakeProfitPips` | Incremento entre niveles consecutivos de toma de ganancias (capas 1-4). | `50` |
| `StochasticLevel` | Umbral aplicado al valor estocástico %K. | `50` |
| `StochasticLength` | Período retrospectivo del cálculo estocástico de %K. | `5` |
| `CandleType` | Serie de velas fuente utilizada por la estrategia (el valor predeterminado es velas de 4 horas). | `4h time-frame` |

## Notas de implementación
- Las señales se evalúan solo en velas terminadas para mantener la coherencia con el experto de MT4 que trabaja en barras nuevas.
- La conversión de pips se adapta automáticamente a símbolos forex de 3/5 dígitos multiplicando el paso mínimo del precio por 10 cuando sea necesario.
- Las entradas y salidas escalonadas se manejan en la memoria mediante objetos en capas para que la estrategia pueda cerrar adecuadamente partes de la posición.
- Todos los comentarios dentro del código C# están escritos en inglés, como lo exigen las pautas del repositorio.
