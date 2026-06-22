# Estrategia Universal de Stop Trailing con Cobertura
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que demuestra diferentes técnicas de stop trailing para proteger posiciones abiertas.
Ofrece stops trailing basados en ATR, Parabolic SAR, media móvil, porcentaje y pips fijos.
Una entrada simple basada en la dirección de la vela se usa únicamente con fines educativos.

## Detalles

- **Criterios de entrada**: Largo si la vela cierra por encima de la apertura, corto si cierra por debajo
- **Largo/Corto**: Ambos
- **Criterios de salida**: Stop trailing activado
- **Stops**: ATR, Parabolic SAR, Media Móvil, Porcentaje de ganancia o pips fijos según el modo seleccionado
- **Valores predeterminados**:
  - `Mode` = `TrailingModes.Atr`
  - `Delta` = 10
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1m
  - `SarStep` = 0.02m
  - `SarMax` = 0.2m
  - `MaPeriod` = 34
  - `PercentProfit` = 50m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Gestión de riesgo
  - Dirección: Ambos
  - Indicadores: ATR, Parabolic SAR, SMA
  - Stops: Stop trailing
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
