# Estrategia de Seguimiento de Tendencia KNN
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trend Following KNN es una estrategia simplificada que mide el cambio promedio de precio en una ventana y compara el precio con una media móvil.
Compra cuando el cambio promedio es positivo y el precio está por encima de la media móvil, vende cuando el cambio promedio es negativo y el precio está por debajo de la media móvil.

## Detalles

- **Criterios de entrada**: cambio promedio positivo/negativo con precio por encima/por debajo de la media móvil
- **Largo/Corto**: Ambos
- **Criterios de salida**: señal opuesta
- **Stops**: No
- **Valores predeterminados**:
  - `WindowSize` = 20
  - `MaLength` = 50
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: SMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
