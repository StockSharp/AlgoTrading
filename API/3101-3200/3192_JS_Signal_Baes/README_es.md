# Estrategia JS Signal Baes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es un port de StockSharp del asesor experto de MetaTrader "JS Signal Baes". Evalúa seis marcos temporales diferentes simultáneamente (M1, M5, M15, M30, H1, H4 por defecto) y espera hasta que todos los indicadores monitoreados coincidan en la misma dirección de mercado antes de abrir una posición. Las señales pueden invertirse a través del parámetro **Reverse** para los usuarios que quieran operar en contra de la tendencia detectada.

## Indicadores y confirmaciones
Los siguientes indicadores se calculan en cada uno de los seis marcos temporales:

- **Dos Medias Móviles** usando el método de suavizado seleccionado (simple, exponencial, suavizado o ponderado linealmente).
- **MACD (Moving Average Convergence Divergence)** usando longitudes configurables de rápida, lenta y señal.
- **RSI (Relative Strength Index)** con un parámetro de período dedicado.
- **CCI (Commodity Channel Index)** con su propia longitud de lookback.
- **Oscilador Stochastic** definido por períodos K, D y suavizado.

Un marco temporal se considera **alcista** cuando:

1. MA Rápida > MA Lenta.
2. Línea principal MACD > Línea de señal MACD.
3. RSI > 50.
4. CCI > 0.
5. Stochastic %K > 40.

Un marco temporal se considera **bajista** cuando:

1. MA Rápida < MA Lenta.
2. Línea principal MACD < Línea de señal MACD.
3. RSI < 50.
4. CCI < 0.
5. Stochastic %K < 60.

## Reglas de trading
Una nueva posición neteada se abre solo cuando el marco temporal principal (por defecto M1) cierra y **los seis marcos temporales** son simultáneamente alcistas o bajistas:

- **Entrada larga:** todos los marcos temporales son alcistas. Si *Reverse* está habilitado, la señal se convierte en una entrada corta.
- **Entrada corta:** todos los marcos temporales son bajistas. Si *Reverse* está habilitado, la señal se convierte en una entrada larga.

Las posiciones no se piramizan. La estrategia espera hasta que la posición existente sea cerrada externamente antes de actuar sobre una nueva señal. No hay salidas automáticas más allá de la lógica de señal opuesta del asesor experto original.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| CciPeriod | 13 | Longitud de lookback para el Commodity Channel Index. |
| FastMaPeriod | 5 | Longitud de la media móvil rápida. |
| SlowMaPeriod | 9 | Longitud de la media móvil lenta. |
| MaMethod | LinearWeighted | Tipo de suavizado de media móvil aplicado a ambas medias. |
| MacdFastPeriod | 8 | Longitud EMA rápida usada por MACD. |
| MacdSlowPeriod | 17 | Longitud EMA lenta usada por MACD. |
| MacdSignalPeriod | 9 | Longitud de la línea de señal usada por MACD. |
| StochasticKPeriod | 5 | Período K para el oscilador stochastic. |
| StochasticDPeriod | 3 | Período D para el oscilador stochastic. |
| StochasticSmoothing | 3 | Factor de suavizado para el oscilador stochastic. |
| RsiPeriod | 9 | Longitud de lookback del RSI. |
| ReverseSignals | false | Invertir la dirección de cada señal de trading. |
| TimeFrame1..6 | M1, M5, M15, M30, H1, H4 | Series de velas asignadas a cada marco temporal. |

## Notas
- Los parámetros predeterminados replican la configuración integrada en la versión de MetaTrader.
- La gestión monetaria, stop-loss, take-profit y la lógica de trailing del código original no se reproducen; usar controles de riesgo a nivel de cartera si es necesario.
- Asegúrese de que los datos históricos estén disponibles para cada marco temporal seleccionado para que los indicadores puedan calentarse antes de operar.
