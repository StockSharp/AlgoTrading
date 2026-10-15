# Estrategia Larry Connors Bollinger %B
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia sigue el enfoque %B de Larry Connors. Compra cuando el precio está en una tendencia alcista por encima de la SMA de 200 períodos y el valor de Bollinger %B se mantiene por debajo de un umbral durante tres velas consecutivas. Las posiciones se cierran cuando %B sube por encima de un umbral superior.

La configuración predeterminada apunta a velas diarias.

## Detalles

- **Criterios de entrada**: Cierre por encima de SMA200 y %B por debajo de `LowPercentB` durante tres velas consecutivas.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: %B cruza por encima de `HighPercentB` o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `SmaPeriod` = 200
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `LowPercentB` = 0.2m
  - `HighPercentB` = 0.8m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo
  - Indicadores: Bollinger Bands, SMA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
