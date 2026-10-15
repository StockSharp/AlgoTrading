# Estrategia EMA 10/55/200 Solo Largos MTF
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia abre posiciones largas cuando los cruces de EMA en el gráfico de 4 horas se alinean con tendencias alcistas en los gráficos diarios y semanales.

## Detalles

- **Criterios de entrada**:
  - `EMA10` cruza por encima de `EMA55` con el máximo de la vela por encima de `EMA55`, o `EMA55` cruza por encima de `EMA200`, o `EMA10` cruza por encima de `EMA500`.
  - La `EMA55` diaria está por encima de `EMA200` y la `EMA55` semanal está por encima de `EMA200`.
- **Criterios de salida**:
  - `EMA10` cruza por debajo de `EMA200` o `EMA500`.
  - El precio cae al nivel de stop loss.
- **Parámetros**:
  - `EMA 10 Length` = 10
  - `EMA 55 Length` = 55
  - `EMA 200 Length` = 200
  - `EMA 500 Length` = 500
  - `Stop Loss %` = 5
