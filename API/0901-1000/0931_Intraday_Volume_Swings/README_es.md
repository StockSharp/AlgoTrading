# Estrategia Intradía de Oscilaciones por Volumen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia opera cuando el precio entra en regiones de oscilación definidas por volumen del día actual o del día anterior.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El precio entra en la región de oscilación alta.
  - **Corto**: El precio entra en la región de oscilación baja.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `RegionMustClose` = true
  - `CandleType` = 1 minute
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Volumen
  - Stops: No
  - Complejidad: Medio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
