# Altarius RSI Stochastic Estrategia dual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Altarius RSI Stochastic Estrategia Dual es una conversión del MetaTrader asesor experto `AltariusRSIxampnSTOH`. La lógica combina dos osciladores estocásticos con un filtro RSI de período corto. El estocástico lento identifica la dirección de la tendencia y las zonas de sobrecompra/sobreventa, mientras que el estocástico rápido mide la fuerza del impulso. Las salidas dependen de RSI y de la lenta línea de señal estocástica para seguir las operaciones ganadoras y reducir las pérdidas. Las funciones adicionales de administración de dinero reflejan la lógica original de MQL al reducir el tamaño de la posición después de las pérdidas y aplicar un límite de retiro de capital.

## Lógica de trading

1. **Fuente de datos**: la estrategia funciona con velas configurables (barras predeterminadas de 15 minutos). Todos los cálculos utilizan datos de cierre de velas.
2. **Condiciones de entrada**
   - **Configuración larga**: la línea principal estocástica lenta (15,8,8) está por encima de su línea de señal pero aún por debajo de `BuyStochasticLimit` (50 por defecto). El estocástico rápido (10,3,3) muestra el impulso con una diferencia absoluta entre las líneas principal y de señal por encima del `StochasticDifferenceThreshold` (5 por defecto).
   - **Configuración corta**: La línea principal estocástica lenta está por debajo de su línea de señal pero permanece por encima del `SellStochasticLimit` (55 por defecto). El estocástico rápido debe volver a mostrar una diferencia mayor que el umbral de impulso.
3. **Condiciones de salida**
   - **Salida larga**: Se activa cuando el RSI (período 4) excede `ExitRsiHigh` (60) y la línea de señal estocástica lenta desciende por debajo de su valor anterior mientras se mantiene por encima de `ExitStochasticHigh` (70).
   - **Salida corta**: Se activa cuando el RSI cae por debajo de `ExitRsiLow` (40) y la línea de señal estocástica lenta se eleva por encima de su valor anterior mientras permanece por debajo de `ExitStochasticLow` (30).
   - **Salida de riesgo**: si el PnL flotante cae por debajo de la reducción de capital permitida (`MaximumRiskPercent`), todas las posiciones se nivelan inmediatamente.
4. **Tamaño de posición**: comienza con `BaseVolume` y reduce el tamaño efectivo después de operaciones perdedoras consecutivas a través de `DecreaseFactor`. Las restricciones de volumen del corredor se respetan mediante el paso y los límites del volumen de seguridad.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `BaseVolume` | Tamaño base del pedido antes de los ajustes de gestión de riesgos. |
| `MaximumRiskPercent` | Porcentaje del capital de la cuenta que se puede perder antes de que la estrategia cierre posiciones por la fuerza. |
| `DecreaseFactor` | Divisor que controla la rapidez con la que el tamaño de la posición se contrae después de pérdidas consecutivas. |
| `RsiPeriod` | RSI longitud utilizada para las decisiones de salida. |
| `SlowStochasticPeriod`, `SlowStochasticK`, `SlowStochasticD` | Configuración del oscilador estocástico lento que impulsa la dirección de la tendencia. |
| `FastStochasticPeriod`, `FastStochasticK`, `FastStochasticD` | Configuración del oscilador estocástico rápido que mide el impulso. |
| `StochasticDifferenceThreshold` | Distancia mínima entre las líneas estocásticas principales y de señal rápidas para confirmar el impulso. |
| `BuyStochasticLimit`, `SellStochasticLimit` | Niveles estocásticos lentos que definen la zona comercial aceptable para nuevas posiciones. |
| `ExitRsiHigh`, `ExitRsiLow` | RSI niveles que preparan salidas largas o cortas. |
| `ExitStochasticHigh`, `ExitStochasticLow` | Niveles de señal estocásticos lentos que finalizan las salidas. |
| `CandleType` | Fuente de datos de velas para cálculos de indicadores. |

## Notas

- La estrategia negocia una sola posición a la vez, reflejando el comportamiento original del asesor experto.
- Los ajustes de volumen y la protección contra caídas se calculan utilizando la información actual de la cartera disponible en StockSharp.
- La visualización del gráfico dibuja velas, tanto osciladores estocásticos como marcadores comerciales cuando hay un área del gráfico disponible.
