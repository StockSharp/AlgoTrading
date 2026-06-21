# Estrategia Color Zerolag Momentum OSMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia construye un oscilador OSMA de Momentum de cero rezago personalizado utilizando cinco cálculos de Momentum.
Cuando el valor del oscilador de hace dos barras está por debajo del valor de hace tres barras, la tendencia se considera alcista.
En este caso, las posiciones cortas se cierran y se puede abrir una nueva posición larga si el valor más reciente está por encima del valor de hace dos barras.
Cuando el valor de hace dos barras está por encima del valor de hace tres barras, la tendencia es bajista, las posiciones largas se cierran y se puede abrir una corta si el último valor está por debajo del valor de hace dos barras.

## Parámetros

- `Smoothing1` – primer factor de suavizado para la tendencia lenta.
- `Smoothing2` – segundo factor de suavizado para la línea OSMA.
- `Factor1-5` – pesos aplicados a cada componente de Momentum.
- `MomentumPeriod1-5` – períodos para los indicadores de Momentum.
- `CandleType` – marco temporal de velas para los cálculos.
- `BuyOpen` – permitir abrir posiciones largas.
- `SellOpen` – permitir abrir posiciones cortas.
- `BuyClose` – permitir cerrar posiciones largas.
- `SellClose` – permitir cerrar posiciones cortas.
