# Estrategia de Rotación de Momentum Sectorial
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Rotación de Momentum Sectorial** rota el capital entre ETFs sectoriales. Al final de cada mes se calcula el retorno histórico de cada sector en varias ventanas de retroceso. El sistema compra los sectores más fuertes y sale de los más débiles, manteniendo exposición solo a los de mejor desempeño.

## Detalles
- **Criterios de entrada**: Clasificación mensual del momentum de ETFs sectoriales.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Rebalanceo mensual cuando cambian los rankings.
- **Stops**: Sin stop explícito.
- **Valores predeterminados**:
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Largo
  - Indicadores: Basados en precio
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Medio plazo
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
