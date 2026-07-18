# Estrategia NonLagDot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia inspirada en el indicador NonLagDot. El indicador aproxima la tendencia del precio utilizando una media móvil suavizada y puntos codificados por colores.
La estrategia abre una posición larga cuando el indicador gira hacia arriba y una posición corta cuando gira hacia abajo.
Las posiciones opuestas anteriores se cierran antes de abrir una nueva.

## Detalles

- **Criterios de entrada**:
  - Largo: el indicador pasa de bajista a alcista (la pendiente de la media móvil se vuelve positiva)
  - Corto: el indicador pasa de alcista a bajista (la pendiente se vuelve negativa)
- **Largo/Corto**: Ambos
- **Criterios de salida**: señal opuesta
- **Stops**: porcentaje de stop-loss opcional
- **Valores predeterminados**:
  - `Length` = 10
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
  - `StopLossPercent` = 1m
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: aproximación de la pendiente SMA de NonLagDot
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
