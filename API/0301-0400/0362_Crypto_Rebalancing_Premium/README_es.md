# Estrategia de Prima de Rebalanceo Cripto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia de Prima de Rebalanceo Cripto mantiene una cartera de igual ponderación entre Bitcoin y Ethereum. Al rebalancear la cesta cada semana, intenta capturar la prima generada por la volatilidad entre los dos activos.

La estrategia observa velas por hora y realiza un rebalanceo en la primera hora de cada lunes. Las operaciones se omiten si el ajuste requerido es menor que un umbral en USD definido por el usuario.

## Detalles

- **Universo**: Símbolos de Bitcoin y Ethereum.
- **Señal**: Mantener BTC y ETH en pesos 50/50.
- **Rebalanceo**: Semanal, el lunes a las 00:00 UTC.
- **Posicionamiento**: Solo largos, igual ponderación.
- **Parámetros**:
  - `BTC` – activo Bitcoin.
  - `ETH` – activo Ethereum.
  - `MinTradeUsd` – valor mínimo de operación en USD.
  - `CandleType` – marco temporal de las velas (predeterminado: 1 hora).
- **Nota**: La implementación es simplificada y no incluye comisiones ni costes de financiación.
