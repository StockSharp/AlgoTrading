# Estrategia Parabolic RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que aplica Parabolic SAR al RSI para detectar cambios de tendencia. La estrategia entra cuando el SAR gira respecto a la línea del RSI y puede filtrar operaciones mediante umbrales de RSI.

## Detalles

- **Criterios de entrada**:
  - Largo: `SAR` gira por debajo del RSI y (opcional) `RSI ≥ LongRsiMin`
  - Corto: `SAR` gira por encima del RSI y (opcional) `RSI ≤ ShortRsiMax`
- **Largo/Corto**: Configurable
- **Criterios de salida**: Giro contrario del SAR
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `RsiLength` = 14
  - `SarStart` = 0.02
  - `SarIncrement` = 0.02
  - `SarMax` = 0.2
  - `LongRsiMin` = 50
  - `ShortRsiMax` = 50
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Configurable
  - Indicadores: Parabolic SAR, RSI
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
