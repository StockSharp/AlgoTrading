# Estrategia CCI de Soporte y Resistencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia usa pivotes de CCI para construir niveles dinámicos de soporte y resistencia. Se aplica un filtro de tendencia basado en el cruce o pendiente de EMA antes de operar rupturas de estos niveles.

## Detalles

- **Criterios de entrada**:
  - Largo: el precio cierra por encima del soporte basado en CCI tras tocarlo y la tendencia es alcista.
  - Corto: el precio cierra por debajo de la resistencia basada en CCI tras tocarla y la tendencia es bajista.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Stop loss y take profit basados en ATR.
- **Stops**: Sí, basados en ATR.
- **Valores predeterminados**:
  - `CciLength` = 50
  - `LeftPivot` = 50
  - `RightPivot` = 50
  - `Buffer` = 10
  - `TrendMatter` = true
  - `TrendType` = Cross
  - `SlowMaLength` = 100
  - `FastMaLength` = 50
  - `SlopeLength` = 5
  - `Ksl` = 1.1
  - `Ktp` = 2.2
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: CCI, EMA, ATR
  - Stops: Sí
  - Complejidad: Medio
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
