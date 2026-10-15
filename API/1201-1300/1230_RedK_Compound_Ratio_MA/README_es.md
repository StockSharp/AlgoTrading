# Estrategia RedK de MA de Ratio Compuesto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Opera en largo cuando la media móvil de ratio compuesto (CoRa Wave) sube y en corto cuando cae.

## Detalles

- **Criterios de entrada**:
  - Largo: El valor de CoRa Wave sube por encima del valor anterior
  - Corto: El valor de CoRa Wave cae por debajo del valor anterior
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Señal opuesta
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `Length` = 20
  - `RatioMultiplier` = 2m
  - `AutoSmoothing` = true
  - `ManualSmoothing` = 1
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Compound Ratio MA, Weighted Moving Average
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Corto plazo
  - Estacionalidad: Ninguno
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
