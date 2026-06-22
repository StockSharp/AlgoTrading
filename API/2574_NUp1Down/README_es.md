# Estrategia NUp1Down
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia NUp1Down** es una conversión directa del experto de MetaTrader 5 "N bars up, then one bar down" (archivo `NUp1Down.mq5`). Escanea velas completadas entregadas por StockSharp y entra en una operación corta cuando aparece una vela bajista después de una secuencia configurable de velas alcistas que siguen haciendo cierres más altos. La estrategia está diseñada para traders discrecionales que quieren automatizar un patrón clásico de reversión de swing dentro de StockSharp Designer, Shell o Runner.

## Lógica de trading
1. Trabajar solo en velas terminadas proporcionadas por el parámetro `CandleType`.
2. Mantener en memoria las últimas `BarsCount + 1` velas. La vela más nueva debe cerrar por debajo de su apertura (vela de configuración bajista).
3. Las `BarsCount` velas anteriores deben cerrar por encima de sus aperturas. Cada una de estas velas alcistas (excepto la más antigua) también debe cerrar por encima del cierre de la vela que vino justo antes, imponiendo un movimiento "escalera" al alza.
4. Cuando el patrón se valida y no hay posición corta activa, la estrategia envía una orden de venta a mercado.
5. El dimensionamiento de la posición usa el parámetro `RiskPercent`. El algoritmo estima cuántos contratos se pueden abrir para que el capital en riesgo (distancia al stop-loss convertida a valor monetario) no exceda el porcentaje elegido del portafolio. La propiedad base `Volume` sigue siendo el tamaño mínimo de lote y el modelo de riesgo solo puede aumentar el tamaño de la operación.

## Gestión de posición
- Al entrar se calculan un stop-loss protector y un nivel de take-profit desde el precio de entrada. Ambas distancias se expresan en pips y se traducen a precios usando el `PriceStep` del instrumento. Para símbolos con tres o cinco dígitos decimales, el tamaño del pip se ajusta automáticamente para coincidir con la definición de pip de MetaTrader.
- Un stop trailing se recalcula en cada vela terminada. La distancia de trailing es igual a `TrailingStopPips` y el stop se desplaza solo si el precio se ha movido al menos `TrailingStepPips` a favor de la operación. La lógica de trailing emula el experto original: para operaciones cortas sigue el precio de demanda más bajo, mientras que las operaciones largas no son producidas por esta estrategia.
- Las condiciones de salida se evalúan antes de buscar nuevas entradas en cada vela. La estrategia cierra la posición cuando se golpea el stop-loss o el take-profit, o cuando la lógica de trailing aprieta el stop por encima del precio de demanda actual.

## Parámetros
| Nombre | Descripción |
| ------ | ----------- |
| `BarsCount` | Número de velas alcistas requeridas antes de la vela de configuración bajista (predeterminado: 3). |
| `TakeProfitPips` | Distancia de take-profit en pips aplicada al precio de entrada (predeterminado: 50). |
| `StopLossPips` | Distancia de stop-loss en pips aplicada al precio de entrada (predeterminado: 50). |
| `TrailingStopPips` | Distancia entre el precio de mercado y el stop trailing (predeterminado: 10). |
| `TrailingStepPips` | Movimiento favorable mínimo antes de que avance el stop trailing (predeterminado: 5). |
| `RiskPercent` | Porcentaje del capital del portafolio a arriesgar en cada operación (predeterminado: 5). |
| `CandleType` | Tipo de datos / marco temporal de velas usado para la detección del patrón (predeterminado: 1 hora). |

## Notas de uso
- Configure la propiedad `Volume` al tamaño mínimo de orden permitido por su broker. El dimensionamiento basado en riesgo puede aumentar el tamaño de la operación pero nunca lo reduce por debajo de `Volume`.
- La estrategia mantiene solo una posición corta agregada a la vez. Si existe una posición larga, se cerrará antes de abrir la corta.
- El algoritmo trabaja con datos de velas. Los hits de stop-loss o take-profit intrabar se detectan usando el máximo/mínimo de la vela, por lo que el tiempo de ejecución real puede diferir de la ejecución a nivel de tick.
- No se proporciona versión Python en esta versión. Solo está disponible la implementación C# dentro de `API/2574/CS/NUp1DownStrategy.cs`.
