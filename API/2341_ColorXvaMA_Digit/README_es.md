# Estrategia ColorXvaMA Digit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera basándose en el cambio de pendiente de una media móvil con doble suavizado. Una Media Móvil Exponencial se suaviza nuevamente con una Media Móvil Jurik. Se abre una posición larga cuando la JMA rápida cruza por encima de la EMA lenta, y una posición corta cuando cruza por debajo.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La JMA rápida cruza por encima de la EMA lenta.
  - **Corto**: La JMA rápida cruza por debajo de la EMA lenta.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**: Señal opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `SlowLength` = 15
  - `FastLength` = 5
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: EMA, JMA
  - Stops: Ninguno
  - Complejidad: Bajo
  - Marco temporal: 8h
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
