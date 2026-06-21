# Estrategia de Trading Basada en Hull Moving Average MH
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de ruptura basada en Hull Moving Average.

La estrategia compara el precio de apertura con los niveles dinámicos derivados de la Hull Moving Average. Entra largo cuando el precio rompe por encima del nivel superior y corto cuando cae por debajo del nivel inferior. Las posiciones existentes se cierran en rupturas opuestas.

## Detalles

- **Criterios de entrada**: Relación del precio con los niveles de Hull Moving Average.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Ruptura opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `HullPeriod` = 210
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: MA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
