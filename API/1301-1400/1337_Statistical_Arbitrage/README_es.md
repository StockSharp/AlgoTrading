# Estrategia de Arbitraje Estadístico de Diferencial
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera el diferencial entre dos instrumentos correlacionados. Se abre una posición larga en el primer valor cuando el diferencial cae por debajo de su media en un múltiplo de la desviación estándar del diferencial. La posición se cierra una vez que el diferencial regresa a la media.

## Detalles
- **Criterios de entrada**:
  - Largo: Diferencial < Media - Multiplicador * DesvEst
- **Largo/Corto**: Solo largos
- **Criterios de salida**: Cerrar cuando diferencial > Media
- **Stops**: No
- **Valores predeterminados**:
  - `LookbackPeriod` = 20
  - `StdMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Arbitrage
  - Dirección: Largo
  - Indicadores: Estadísticas del diferencial
  - Stops: No
  - Complejidad: Principiante
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
