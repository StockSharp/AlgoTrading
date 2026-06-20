# Estrategia de Prima de Pequeña Capitalización
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Prima de Pequeña Capitalización** captura la tendencia histórica de las acciones de baja capitalización a superar a las de gran capitalización. El universo se divide por capitalización bursátil y la cartera mantiene una cesta de pequeñas capitalizaciones mientras vende en corto un índice de grandes capitalizaciones.

## Detalles
- **Criterios de entrada**: Selección por clasificación de capitalización bursátil.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Rebalanceo periódico.
- **Stops**: Sin stop explícito.
- **Valores predeterminados**:
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Fundamental
  - Dirección: Ambos
  - Indicadores: Fundamentales
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
