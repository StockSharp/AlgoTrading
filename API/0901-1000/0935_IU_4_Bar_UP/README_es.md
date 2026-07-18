# Estrategia IU 4 Barras Alcistas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia IU 4 Barras Alcistas es un enfoque solo largos que compra después de cuatro velas alcistas consecutivas cuando el precio está por encima del indicador SuperTrend.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**:
  - **Largo**: Cuatro velas alcistas consecutivas y cierre por encima de SuperTrend.
- **Criterios de salida**: Cierre por debajo de SuperTrend.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `SupertrendLength` = 14
  - `SupertrendMultiplier` = 1
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo
  - Indicadores: SuperTrend
  - Complejidad: Bajo
  - Nivel de riesgo: Medio
