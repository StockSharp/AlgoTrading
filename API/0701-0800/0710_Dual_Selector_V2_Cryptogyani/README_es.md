# Selector de Estrategia Dual V2 - Estrategia Cryptogyani
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia alterna entre dos enfoques solo largos basados en SMA.

- **Estrategia 1**: opera cruce de SMA con take profit trailing opcional o objetivo fijo.
- **Estrategia 2**: opera cruce de SMA confirmado por tendencia en marco temporal superior, usa stop ATR y take profit parcial.

## Detalles

- **Criterios de entrada**:
  - Estrategia 1: SMA rápida cruza por encima de SMA lenta.
  - Estrategia 2: SMA rápida cruza por encima de SMA lenta y el precio está por encima de la SMA del marco temporal superior.
- **Criterios de salida**:
  - Estrategia 1: objetivo de take profit o stop trailing.
  - Estrategia 2: take profit parcial y luego stop basado en ATR.
- **Indicadores**: SMA, ATR.
- **Dirección**: Solo largos.
- **Stops**: Sí.
