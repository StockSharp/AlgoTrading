# Estrategia Fibonacci Time Zones
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es una adaptación a StockSharp del asesor experto de MetaTrader "Fibonacci Time Zones". Conserva el carácter discrecional del script original al combinar un filtro MACD de marco temporal superior con salidas por bandas de Bollinger y un módulo amplio de gestión monetaria. Todas las rutinas de gestión de operaciones se reescribieron usando la API de alto nivel: la estrategia se suscribe a dos flujos de velas (un marco temporal de trading y un marco temporal más lento para la confirmación MACD) y enlaza indicadores directamente mediante callbacks `Bind`/`BindEx`.

## Lógica central

1. **Filtro de momentum** - Se calcula un histograma MACD mensual (configurable). Un cruce alcista por encima de la línea de señal programa entradas largas, mientras que un cruce bajista programa entradas cortas. La posición real se abre en la siguiente vela de trading para evitar órdenes repetidas en el mismo cruce.
2. **Ejecución de entrada** - Cada señal envía un número de órdenes de mercado definido por el usuario. La exposición opuesta existente se cierra antes de abrir una nueva posición.
3. **Reglas de salida** - Se aplican múltiples capas de defensa:
   - **Salida por banda de Bollinger**: los largos se cierran cuando el precio toca la banda superior; los cortos, cuando se alcanza la banda inferior.
   - **Stop/objetivo clásico**: las distancias estáticas de stop-loss, take-profit y trailing-stop se convierten de pips a unidades de precio y se pasan a `StartProtection`.
   - **Break-even**: después de que el precio recorre un número configurable de pips, el stop se lleva a break-even más un desplazamiento. Si el precio retrocede a ese nivel, la posición se cierra.
   - **Trailing monetario**: se monitorizan el PnL abierto y el realizado. Cuando la ganancia flotante alcanza un umbral, la estrategia empieza a seguirla y cierra todo después de un drawdown configurable.
   - **Objetivos de equity**: objetivos opcionales de ganancia absoluta o porcentual cierran todas las operaciones inmediatamente cuando se cumplen.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `UseTakeProfitMoney`, `TakeProfitMoney` | Cierra todas las posiciones cuando la ganancia combinada (realizada + no realizada) alcanza el importe especificado en la divisa de la cuenta. |
| `UseTakeProfitPercent`, `TakeProfitPercent` | Similar a la opción anterior, pero medido como porcentaje del equity inicial. |
| `EnableTrailingProfit`, `TrailingTakeProfitMoney`, `TrailingStopLossMoney` | Activa el trailing basado en dinero cuando se alcanza el primer umbral y protege las ganancias acumuladas. |
| `UseStop`, `StopLossPips`, `TakeProfitPips`, `TrailingStopPips` | Stop clásico, objetivo y distancias trailing expresadas en pips. |
| `UseMoveToBreakEven`, `WhenToMoveToBreakEven`, `PipsToMoveStopLoss` | Controla el comportamiento de break-even. |
| `NumberOfTrades` | Número de órdenes de mercado enviadas por cada señal (imita el EA original, que podía apilar entradas). |
| `CandleType`, `MacdCandleType` | Marcos temporales para las velas de gestión y el filtro MACD. |

## Diferencias con el EA original

* La gestión de botones en el gráfico y los objetos gráficos Fibonacci no se reproducen; la adaptación a StockSharp se centra exclusivamente en la ejecución sistemática.
* El experto original operaba mediante clics manuales en botones. La adaptación entra automáticamente en cruces MACD para ofrecer una estrategia determinista y apta para backtesting.
* Las funciones de cuenta específicas de MetaTrader se sustituyeron por equivalentes de StockSharp (valores de `Portfolio` y `PnL`).

## Consejos de uso

1. Seleccione tipos de vela adecuados antes de iniciar la estrategia. Los valores predeterminados corresponden a un gráfico de trading de 15 minutos con un filtro MACD mensual.
2. Ajuste las distancias basadas en pips según el tamaño de tick del instrumento. La estrategia convierte internamente pips a precio usando `Security.PriceStep`.
3. Para intervención discrecional, desactive los objetivos automáticos de ganancia y use solo la salida por Bollinger.
