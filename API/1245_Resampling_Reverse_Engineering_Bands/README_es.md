# Bandas de Ingeniería Inversa con Remuestreo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Las Bandas de Ingeniería Inversa con Remuestreo realizan ingeniería inversa de los niveles de precio del RSI a una tasa de remuestreo configurable. La estrategia compra cuando el precio cae por debajo de la banda baja y vende cuando el precio sube por encima de la banda alta.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**:
  - **Largo**: el precio de cierre cruza por debajo de la banda baja RRSI.
  - **Corto**: el precio de cierre cruza por encima de la banda alta RRSI.
- **Criterios de salida**: señal opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `RsiPeriod` = 14
  - `HighThreshold` = 70
  - `LowThreshold` = 30
  - `SampleLength` = 1
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Largo y Corto
  - Indicadores: RSI
  - Complejidad: Moderado
  - Nivel de riesgo: Medio
