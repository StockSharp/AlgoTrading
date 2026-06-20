# Estrategia de Factor de Valor por País
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia de Factor de Valor por País clasifica los mercados de renta variable según el ratio CAPE de Shiller. Los países con el CAPE más bajo se consideran baratos y se compran, mientras que los mercados caros se evitan. El enfoque aprovecha la tendencia de los mercados infravalorados a superar al resto con el tiempo.

Cada mes la estrategia redistribuye el capital de forma equitativa entre los países más baratos del universo definido por el usuario. Las posiciones se dimensionan por el valor de la cartera y solo se ejecutan cuando la operación supera un importe mínimo en USD.

## Detalles

- **Universo**: Colección de ETFs de renta variable por país.
- **Señal**: Comprar los países con los ratios CAPE más bajos.
- **Rebalanceo**: Primer día de negociación de cada mes.
- **Posicionamiento**: Solo largos.
- **Parámetros**:
  - `Universe` – valores que representan cada país.
  - `MinTradeUsd` – importe mínimo en dólares por orden.
  - `CandleType` – marco temporal de las velas (predeterminado: 1 día).
- **Nota**: El código de ejemplo contiene lógica de marcador de posición y debe ampliarse con cálculos de factores reales.
