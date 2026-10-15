# Estrategia de Ratio de Bias
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Ratio de Bias opera rupturas basadas en la desviación del precio respecto a medias móviles de largo plazo. Compara el precio de cierre con una media móvil exponencial (EMA) y una media móvil simple (SMA). Se abre una posición larga cuando el precio supera la EMA en un ratio especificado, y una posición corta cuando el precio cae por debajo de la SMA en el mismo ratio.

## Detalles

- **Criterios de entrada**:
  - `close / EMA >= 1 + BiasThreshold` → entrar en largo.
  - `close / SMA <= 1 - BiasThreshold` → entrar en corto.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**:
  - La señal opuesta cierra y revierte las posiciones.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `MaPeriod` = 200
  - `BiasThreshold` = 0.025
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo y Corto
  - Indicadores: EMA, SMA
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
