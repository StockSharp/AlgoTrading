# Estrategia RSI Stochastic MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina un filtro de tendencia de media móvil simple (SMA) con los osciladores RSI y Stochastic.
La media móvil define la tendencia del mercado. Cuando el precio está por encima de la SMA, la estrategia busca entradas largas;
cuando está por debajo de la SMA, busca entradas cortas. Los niveles de RSI y Stochastic identifican condiciones de sobreventa o
sobrecompra para sincronizar las entradas.

Las posiciones se cierran cuando los osciladores abandonan sus zonas extremas. Esto mantiene las operaciones alineadas con
la tendencia predominante evitando movimientos prolongados contra los indicadores.

## Parámetros
- `RsiPeriod` – período de cálculo del RSI.
- `RsiUpperLevel` – umbral de sobrecompra del RSI.
- `RsiLowerLevel` – umbral de sobreventa del RSI.
- `MaPeriod` – período de la media móvil de tendencia.
- `StochKPeriod` – período %K del oscilador Stochastic.
- `StochDPeriod` – período de suavizado %D del oscilador Stochastic.
- `StochUpperLevel` – nivel de sobrecompra del Stochastic.
- `StochLowerLevel` – nivel de sobreventa del Stochastic.
- `Volume` – volumen de la orden.
- `CandleType` – tipo de datos de velas utilizados para los cálculos.

## Indicadores
- Media Móvil Simple
- Índice de Fuerza Relativa
- Oscilador Stochastic

## Reglas de trading
- **Comprar** cuando el precio está por encima de la SMA, el RSI está por debajo de `RsiLowerLevel` y ambas líneas del Stochastic están por debajo de `StochLowerLevel`.
- **Vender** cuando el precio está por debajo de la SMA, el RSI está por encima de `RsiUpperLevel` y ambas líneas del Stochastic están por encima de `StochUpperLevel`.
- **Salir del largo** cuando RSI o Stochastic sube por encima de sus niveles superiores.
- **Salir del corto** cuando RSI o Stochastic cae por debajo de sus niveles inferiores.
