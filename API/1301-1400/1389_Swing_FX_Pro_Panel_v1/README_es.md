# Estrategia Swing FX Pro Panel v1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de demostración que utiliza un cruce de EMA con estadísticas de rendimiento básicas. La EMA rápida cruzando por encima de la EMA lenta abre una posición larga, mientras que un cruce hacia abajo abre una posición corta. Cada operación usa objetivos fijos de ganancia y pérdida.

## Detalles

- **Indicadores**: EMA
- **Parámetros**:
  - `Initial Capital` – tamaño inicial de la cuenta para estadísticas.
  - `Risk Per Trade` – porcentaje de riesgo por operación (informativo).
  - `Analysis Period` – duración del período utilizado para el análisis.
  - `Fast Length` – período de la EMA rápida.
  - `Slow Length` – período de la EMA lenta.
  - `Profit Target` – beneficio en unidades de precio.
  - `Stop Loss` – pérdida en unidades de precio.
