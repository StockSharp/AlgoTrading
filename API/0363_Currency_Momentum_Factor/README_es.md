# Estrategia de Factor de Momentum de Divisas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia de factor clasifica las divisas por su momentum a medio plazo y construye una cartera largo/corto. Las divisas con el mejor rendimiento durante la ventana de lookback se compran, mientras que las más débiles se venden en corto en tamaños iguales.

El momentum se evalúa utilizando velas diarias y el libro se rebalancea el primer día de negociación de cada mes. Las órdenes menores a un valor mínimo en USD se ignoran para reducir el ruido.

## Detalles

- **Universo**: Lista de pares de divisas o ETFs.
- **Señal**: Ir largo en las `K` divisas con mayor momentum y corto en las `K` más débiles.
- **Lookback**: Rendimiento calculado sobre `Lookback` velas diarias (predeterminado 252).
- **Rebalanceo**: Mensual.
- **Posicionamiento**: Largo/Corto, dólar neutral.
- **Parámetros**:
  - `Universe` – símbolos de divisas negociables.
  - `Lookback` – número de velas para el momentum.
  - `K` – cantidad de activos a ir largo y corto.
  - `MinTradeUsd` – tamaño mínimo de operación.
  - `CandleType` – marco temporal de las velas (predeterminado: 1 día).
- **Nota**: El ejemplo carece de cálculo real de momentum con fines de demostración.
