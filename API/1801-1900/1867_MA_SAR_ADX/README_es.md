# Estrategia MA SAR ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina Media Móvil, Parabolic SAR e Índice Direccional Promedio (ADX).
Compra cuando el precio está por encima tanto de la media móvil como del SAR y el +DI está por encima del -DI.
Vende cuando el precio está por debajo tanto de la media móvil como del SAR y el +DI está por debajo del -DI.
Las posiciones se cierran cuando el precio cruza el SAR.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close > MA && +DI >= -DI && Close > SAR`
  - Corto: `Close < MA && +DI <= -DI && Close < SAR`
- **Largo/Corto**: Ambos
- **Criterios de salida**: El precio cruza el Parabolic SAR
- **Stops**: No
- **Valores predeterminados**:
  - `MaPeriod` = 100
  - `AdxPeriod` = 14
  - `SarStep` = 0.02m
  - `SarMax` = 0.1m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: SMA, Parabolic SAR, ADX
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
