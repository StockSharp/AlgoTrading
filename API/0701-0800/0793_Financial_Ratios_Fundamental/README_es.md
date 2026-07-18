# Estrategia Fundamental de Ratios Financieros
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia analiza los ratios financieros trimestrales para evaluar los fundamentos de una empresa. Examina el ratio de liquidez corriente, la cobertura de intereses, la rotación de cuentas por pagar y el margen bruto, entrando en posiciones largas cuando cualquiera de estos ratios mejora respecto al período anterior.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `currentRatio > previousCurrent` O `interestCoverage < previousInterest` O `payableTurnover > previousPayable` O `grossMargin > previousGross`.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - **Largo**: `currentRatio < previousCurrent` O `interestCoverage > previousInterest` O `payableTurnover < previousPayable` O `grossMargin < previousGross`.
- **Stops**: No.
- **Valores predeterminados**:
  - `Candle Type` = velas diarias.
- **Filtros**:
  - Categoría: Fundamental
  - Dirección: Solo largos
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Largo plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
