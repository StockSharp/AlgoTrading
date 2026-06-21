# Estrategia de Indicador de Tipo de Tendencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trend Type Indicator detecta el régimen de mercado usando ATR y ADX.
Va largo durante las tendencias alcistas, corto durante las bajistas y sale cuando las condiciones se vuelven laterales.

## Detalles

- **Criterios de entrada**: +DI mayor que -DI y sin movimiento lateral
- **Largo/Corto**: Ambos
- **Criterios de salida**: Tendencia opuesta o lateral
- **Stops**: No
- **Valores predeterminados**:
  - `UseAtr` = true
  - `AtrLength` = 14
  - `AtrMaLength` = 20
  - `UseAdx` = true
  - `AdxLength` = 14
  - `AdxLimit` = 25
  - `SmoothFactor` = 3
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: ATR, ADX
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
