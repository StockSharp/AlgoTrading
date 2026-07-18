# Estrategia Alexav SpeedUp M1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Conversión del asesor experto "Alexav SpeedUp M1" de MetaTrader 5 a la API de alto nivel de StockSharp.
- Diseñado para mercados rápidos en el marco temporal de 1 minuto (predeterminado) y reacciona a cuerpos de vela inusualmente grandes.
- Abre una única posición neta en la dirección del cuerpo de la vela fuerte y la gestiona con stop-loss fijo, take-profit y un trailing stop escalonado.
- Utiliza entradas basadas en pips que se convierten automáticamente a distancias de precio según el tamaño del tick del instrumento y la precisión decimal.

## Idea original vs. implementación en StockSharp
- El EA original abría operaciones largas y cortas simultáneamente en cuentas de cobertura. Las estrategias de StockSharp operan en un entorno de neteo, por lo que este port mantiene solo una posición a la vez y entra en la dirección de la vela grande.
- La lógica del trailing stop sigue la versión MT5: espera que el precio se mueva `TrailingStop + TrailingStep` antes de acercar el stop la distancia de trailing, y solo actualiza cuando el precio avanza al menos un trailing step más allá del stop anterior.
- Las distancias en pips se convierten a unidades de precio multiplicando por el tamaño mínimo del tick. Para símbolos Forex de 3 o 5 decimales, el código multiplica el tick por 10 para emular el manejo de pips de MT5.

## Reglas de entrada
1. Trabajar con velas terminadas del marco temporal seleccionado (predeterminado: 1 minuto).
2. Medir el cuerpo de la vela: `abs(Close - Open)`.
3. Si el cuerpo supera `MinimumBodySizePips * pipSize` y no hay posición activa, entrar en la dirección del cuerpo de la vela:
   - Vela alcista → abrir posición larga.
   - Vela bajista → abrir posición corta.

## Reglas de salida
- **Stop-loss** – colocado a `StopLossPips * pipSize` del precio de entrada. Deshabilitado cuando el parámetro es cero.
- **Take-profit** – colocado a `TakeProfitPips * pipSize` de la entrada. Deshabilitado cuando el parámetro es cero.
- **Trailing stop** – habilitado cuando `TrailingStopPips > 0` y `TrailingStepPips > 0`.
  - Se activa después de que la operación gana al menos `TrailingStopPips + TrailingStepPips` pips.
  - Para operaciones largas, el stop se mueve a `Close - TrailingStopPips * pipSize` cuando se cumple la condición y el precio avanzó al menos un trailing step más allá del stop anterior.
  - Para operaciones cortas, el stop se mueve a `Close + TrailingStopPips * pipSize` usando la misma condición de paso.

## Parámetros
- `OrderVolume` – tamaño de la operación en lotes (predeterminado `0.1`).
- `StopLossPips` – distancia del stop-loss en pips (predeterminado `30`).
- `TakeProfitPips` – distancia del take-profit en pips (predeterminado `90`).
- `TrailingStopPips` – distancia del trailing stop en pips (predeterminado `10`).
- `TrailingStepPips` – movimiento favorable mínimo antes de que se actualice el trailing stop (predeterminado `5`). Debe ser mayor que cero cuando el trailing stop está habilitado.
- `MinimumBodySizePips` – tamaño mínimo del cuerpo (en pips) necesario para activar una operación (predeterminado `100`).
- `CandleType` – marco temporal para velas (predeterminado `1 Minute`).

## Visualización
- La estrategia dibuja automáticamente la serie de velas seleccionada y las propias operaciones en el área del gráfico cuando hay una disponible, simplificando la inspección de señales durante las pruebas.

## Notas de uso
- La configuración predeterminada refleja los ajustes de MT5. Ajuste las distancias en pips para adaptarlas a la volatilidad del instrumento negociado.
- Dado que solo se admite una posición neta, evite ejecutar la estrategia en cuentas de cobertura que esperan posiciones largas y cortas simultáneas.
- Para mercados con tamaños de tick más grandes, reduzca las entradas basadas en pips en consecuencia para mantener distancias de precio comparables.
