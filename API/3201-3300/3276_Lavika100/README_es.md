# Estrategia Lavika100 (StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **estrategia Lavika100** es una adaptación fiel del asesor experto de MetaTrader 5 "Lavika  cent". El sistema combina un filtro de momentum RAVI de una hora (H1) y cuatro horas (H4) para decidir cuándo abrir operaciones. Mantiene las opciones originales de gestión monetaria (lote fijo o porcentaje de riesgo), disciplina de una sola posición, inversión opcional de señales y gestión automática de stops. La versión StockSharp cumple las directrices de la API de alto nivel: las suscripciones a velas impulsan el flujo de trabajo, los indicadores se acceden mediante binders y las órdenes de protección se configuran con `StartProtection`.

## Flujo de trabajo
1. **Suscripciones de datos** - la estrategia se suscribe a velas H1 para el marco temporal de ejecución y a velas H4 para el filtro de tendencia. El indicador `SimpleMovingAverage` se aplica a los precios de apertura para emular las llamadas MT5 `iMA(..., PRICE_OPEN)`.
2. **Momentum RAVI** - dos medias móviles en cada marco temporal (rápida/lenta) generan un porcentaje "RAVI": `(fast - slow) / slow * 100`. El valor H1 debe ser positivo antes de considerar cualquier operación.
3. **Detección del patrón de tendencia** - se inspeccionan los cuatro valores RAVI más recientes en H4:
   - Una secuencia ascendente (`r0 > r1`, `r1 < r2`, `r2 < r3`) dispara una señal larga.
   - Una secuencia descendente (`r0 < r1`, `r1 > r2`, `r2 > r3`) dispara una señal corta. Esto refleja el comportamiento del código original aunque el experto solo cambiaba de dirección mediante el flag `Reverse`.
4. **Inversión de señales y cierre a cero** - según los parámetros `ReverseSignals` y `CloseOpposite`, el algoritmo abre en la dirección detectada o la invierte, cerrando antes cualquier posición opuesta.
5. **Gestión monetaria** - el volumen se toma de `FixedVolume` o se escala por riesgo mediante el método `RiskPercent` (valor de cartera * porcentaje / distancia al stop).
6. **Protección** - stop-loss, take-profit, trailing stop y paso trailing se activan mediante `StartProtection` tan pronto como la estrategia inicia y los parámetros son distintos de cero.

## Reglas de trading
- **Entrada larga** - RAVI H1 es positivo y la serie H4 muestra un patrón ascendente. La estrategia cierra una posición corta existente cuando `CloseOpposite=true` antes de comprar.
- **Entrada corta** - RAVI H1 es positivo y la serie H4 muestra un patrón descendente. Cuando `ReverseSignals=true`, las direcciones se intercambian para coincidir con el interruptor "Reverse" de MT5.
- **Posición única** - con `OnlyOnePosition=true`, cualquier exposición no plana bloquea entradas adicionales hasta que se cierre la posición.
- **Dimensionamiento de volumen** - el modo de porcentaje de riesgo usa el par `PriceStep`/`StepPrice` del instrumento para convertir la distancia de precio en valor monetario, respetando `VolumeStep`, `VolumeMin` y `VolumeMax`.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `H1CandleType` | Marco temporal para la lógica de ejecución (predeterminado 1 hora). |
| `H4CandleType` | Marco temporal superior usado por el filtro de tendencia (predeterminado 4 horas). |
| `H1FastPeriod` / `H1SlowPeriod` | Longitudes de media móvil para el RAVI H1. |
| `H4FastPeriod` / `H4SlowPeriod` | Longitudes de media móvil para el RAVI H4. |
| `StopLossPoints` | Distancia de stop-loss en puntos basados en pips. |
| `TakeProfitPoints` | Distancia de take-profit en puntos basados en pips. |
| `TrailingStopPoints` | Distancia del trailing stop. Establecer en cero para desactivar trailing. |
| `TrailingStepPoints` | Paso mínimo para actualizaciones trailing. Debe ser positivo cuando trailing está habilitado. |
| `FixedVolume` | Tamaño de lote usado en modo fijo. |
| `RiskPercent` | Porcentaje del valor de cartera a arriesgar cuando `MoneyMode` es igual a `RiskPercent`. |
| `MoneyMode` | Cambia entre `FixedLot` y `RiskPercent`. |
| `OnlyOnePosition` | Permite solo una posición abierta. |
| `ReverseSignals` | Invierte acciones largas/cortas (predeterminado true para coincidir con la configuración del EA). |
| `CloseOpposite` | Cierra una posición opuesta antes de colocar una nueva orden. |

## Notas de conversión
- La conversión de pips imita al experto MT5: las cotizaciones de tres y cinco dígitos multiplican `PriceStep` por diez para obtener un incremento del tamaño de un pip.
- El historial RAVI se almacena sin colecciones personalizadas - solo cuatro campos anulables - respetando las restricciones del repositorio contra buffers manuales.
- La gestión monetaria evita llamadas `GetValue` de indicadores y usa metadatos de mercado de StockSharp para mapear el riesgo porcentual a volumen.
- `StartProtection` solo se llama cuando al menos una de las distancias de protección es positiva, garantizando una ejecución segura durante backtests y trading en vivo.

## Consejos de uso
- Proporcione un instrumento de estilo Forex con `PriceStep`, `StepPrice`, `VolumeStep`, `VolumeMin` y `VolumeMax` correctamente configurados.
- Cuando use dimensionamiento basado en riesgo, defina un `StopLossPoints` distinto de cero; de lo contrario, el volumen calculado será cero.
- Debido a que el EA original contenía una peculiaridad lógica donde ambos patrones establecían el flag de compra, mantenga `ReverseSignals=true` si necesita reproducir sus operaciones exactas.
