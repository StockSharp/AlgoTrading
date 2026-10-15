# Estrategia Automatizada de Bollinger Bands
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que coloca órdenes límite de compra en la banda inferior de Bollinger Bands y órdenes límite de venta en la banda superior. Las posiciones se cierran cuando el precio toca la banda media. Las órdenes pendientes se actualizan al inicio de cada vela.

## Detalles

- **Criterios de entrada**:
  - Largo: compra límite en la banda inferior de Bollinger Bands
  - Corto: venta límite en la banda superior de Bollinger Bands
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Largo: el precio cruza por encima de la banda media de Bollinger Bands
  - Corto: el precio cruza por debajo de la banda media de Bollinger Bands
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `BbPeriod` = 20
  - `BbDeviation` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Bollinger Bands
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
