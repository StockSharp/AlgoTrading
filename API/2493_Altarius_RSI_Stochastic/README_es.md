# Estrategia Altarius RSI Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La Estrategia Altarius RSI Stochastic es una conversión directa del asesor experto de MetaTrader 5 "Altarius RSI Stohastic" a la API de alto nivel de StockSharp. El sistema sincroniza dos osciladores Stochastic con un RSI rápido de 3 períodos para capturar reversiones de corta duración que ocurren cuando el momentum se comprime y luego se expande nuevamente. La implementación en StockSharp preserva la lógica original de entrada y salida, añadiendo comodidades modernas como parámetros de estrategia, gestión automática del riesgo y dimensionamiento adaptativo de posición.

## Cómo funciona
- **Stochastic primario (15/8/8):** Actúa como filtro de tendencia. Las posiciones largas requieren que la línea %K esté por debajo de 50 y cruce por encima de la línea %D, señalando momentum alcista en una zona neutral a sobrevendida. Las posiciones cortas requieren la condición espejo por encima de 55.
- **Stochastic secundario (10/3/3):** Mide con qué fuerza se desvía %K de %D. Se requiere un diferencial absoluto mínimo de 5 puntos para validar el momentum antes de entrar a una posición.
- **RSI (Período 3):** Controla las salidas. Las posiciones largas se cierran cuando el RSI supera 60 y el %D primario gira hacia abajo desde un nivel superior a 70. Las posiciones cortas se cierran cuando el RSI cae por debajo de 40 y el %D primario gira hacia arriba desde un nivel inferior a 30.
- **Guarda de Drawdown:** Si el PnL flotante cae por debajo del múltiplo de riesgo configurable del patrimonio de la cuenta, la estrategia liquida inmediatamente la posición abierta, de forma similar al stop de emergencia del código original.
- **Dimensionamiento adaptativo:** El volumen inicial se deriva del patrimonio del portafolio multiplicado por el factor `MaximumRisk` y dividido por 1000, siguiendo el enfoque de MT5. Las operaciones perdedoras consecutivas reducen el tamaño de posición según el `DecreaseFactor`, respetando un volumen mínimo negociable.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `CandleType` | Marco temporal utilizado para las suscripciones de velas. | Marco temporal de 5 minutos |
| `BaseVolume` | Volumen de reserva usado cuando la información del portafolio no está disponible. | 0.1 |
| `MinimumVolume` | Volumen mínimo permitido tras todos los cálculos. | 0.1 |
| `MaximumRisk` | Multiplicador de riesgo aplicado al valor del portafolio para el dimensionamiento y la salida por drawdown. | 0.1 |
| `DecreaseFactor` | Divisor que reduce el volumen tras operaciones perdedoras consecutivas. | 3 |
| `PrimaryStochasticLength` | Período de lookback para la línea %K del Stochastic primario. | 15 |
| `PrimaryStochasticKPeriod` | Suavizado para la línea %K primaria. | 8 |
| `PrimaryStochasticDPeriod` | Período para la línea de señal %D primaria. | 8 |
| `SecondaryStochasticLength` | Período de lookback para el Stochastic de confirmación. | 10 |
| `SecondaryStochasticKPeriod` | Suavizado para la línea %K secundaria. | 3 |
| `SecondaryStochasticDPeriod` | Período para la línea %D secundaria. | 3 |
| `DifferenceThreshold` | Diferencial mínimo entre %K y %D secundarios para permitir entradas. | 5 |
| `PrimaryBuyLimit` | Valor máximo de %K primario permitido antes de abrir un largo. | 50 |
| `PrimarySellLimit` | Valor mínimo de %K primario permitido antes de abrir un corto. | 55 |
| `PrimaryExitUpper` | Umbral de %D primario que debe superarse antes de cerrar largos. | 70 |
| `PrimaryExitLower` | Umbral de %D primario que debe caerse por debajo antes de cerrar cortos. | 30 |
| `RsiPeriod` | Longitud de lookback del RSI. | 3 |
| `LongExitRsi` | Nivel de RSI que confirma las salidas de largos. | 60 |
| `ShortExitRsi` | Nivel de RSI que confirma las salidas de cortos. | 40 |

## Reglas de trading
1. **Criterios de entrada**
   - **Largo:** %K primario > %D primario, %K primario < `PrimaryBuyLimit`, y |%K secundario − %D secundario| > `DifferenceThreshold` mientras la estrategia está plana.
   - **Corto:** %K primario < %D primario, %K primario > `PrimarySellLimit`, y |%K secundario − %D secundario| > `DifferenceThreshold` mientras la estrategia está plana.
2. **Criterios de salida**
   - **Salida largo:** RSI > `LongExitRsi`, %D primario > `PrimaryExitUpper`, y el valor actual de %D es inferior al de la vela anterior.
   - **Salida corto:** RSI < `ShortExitRsi`, %D primario < `PrimaryExitLower`, y el valor actual de %D es superior al de la vela anterior.
   - **Salida por riesgo:** Cuando la pérdida flotante supera `MaximumRisk × Portfolio.CurrentValue`.

## Gestión del riesgo
- La estrategia llama automáticamente a `StartProtection()` para activar los servicios de protección de posición integrados de StockSharp.
- El tamaño de la posición se reduce cuando `_lossStreak` supera una operación perdedora consecutiva, imitando la lógica `DecreaseFactor` de MT5.
- `MinimumVolume` evita que el tamaño de posición caiga por debajo de los requisitos de tamaño de tick del mercado.

## Notas
- La estrategia asume un portafolio con capacidad de cobertura, exactamente como el EA original.
- Personaliza el parámetro `CandleType` para que coincida con el marco temporal que habrías utilizado en MetaTrader (M1, M5, etc.).
- Combina este módulo con StockSharp Designer o el proyecto Backtester en este repositorio para validar el rendimiento con tus propios datos.
