# Estrategia del Factor de Calidad de Ganancias
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Earnings Quality Factor** rebalancea anualmente el 1 de julio, tomando posiciones largas en acciones de alta calidad y cortas en acciones de baja calidad según las puntuaciones de calidad de ganancias.

## Detalles
- **Criterios de entrada**: Rebalanceo anual el 1 de julio usando puntuaciones de calidad.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Próximo rebalanceo anual.
- **Stops**: No.
- **Valores predeterminados**:
  - `MinTradeUsd = 100`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Fundamental
  - Dirección: Ambos
  - Indicadores: Calidad
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Diario
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
