# Estrategia Sangre en las Calles
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia compra cuando la caída actual desde el máximo reciente cae por debajo de un umbral de desviación estándar. La posición se cierra después de un número fijo de barras.

## Detalles

- **Criterios de entrada**:
  - Largo: caída ≤ media + `StdDevThreshold` × desviación estándar
- **Largo/Corto**: Solo largos
- **Criterios de salida**: posición cerrada después de `ExitBars` barras
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `LookbackPeriod` = 50
  - `StdDevLength` = 50
  - `StdDevThreshold` = -1m
  - `ExitBars` = 35
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Largo
  - Indicadores: Highest, SMA, StandardDeviation
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
