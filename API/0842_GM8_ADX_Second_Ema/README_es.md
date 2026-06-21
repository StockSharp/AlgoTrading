# Estrategia GM-8 y ADX con Segunda EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia entra en operaciones cuando el precio cruza una SMA GM-8 y se alinea con una segunda EMA mientras el ADX confirma una tendencia fuerte.

## Detalles

- **Criterios de entrada**:
  - **Largo**: el precio cruza por encima de la SMA y cierra por encima de la SMA y la segunda EMA con ADX por encima del umbral.
  - **Corto**: el precio cruza por debajo de la SMA y cierra por debajo de la SMA y la segunda EMA con ADX por encima del umbral.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - **Largo**: el precio cruza por debajo de la SMA.
  - **Corto**: el precio cruza por encima de la SMA.
- **Stops**: Usa StartProtection.
- **Valores predeterminados**:
  - `GM Period` = 15
  - `Second EMA Period` = 59
  - `ADX Period` = 8
  - `ADX Threshold` = 34
  - `Candle Type` = 15m
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: SMA, EMA, ADX
  - Stops: Sí
  - Complejidad: Bajo
  - Marco temporal: Corto plazo

