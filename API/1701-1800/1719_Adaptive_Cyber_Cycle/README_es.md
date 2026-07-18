# Estrategia Adaptive Cyber Cycle
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza el oscilador Adaptive Cyber Cycle de John Ehlers. Calcula un ciclo de precio suavizado y utiliza el valor anterior como línea de activación. Se abre una posición larga cuando el ciclo cruza hacia arriba la línea de activación, y una posición corta cuando cruza hacia abajo.

## Detalles

- **Criterios de entrada**:
  - **Largo**: ciclo > ciclo anterior.
  - **Corto**: ciclo < ciclo anterior.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - La señal opuesta cierra e invierte la posición.
- **Stops**: Ninguno por defecto; la protección puede habilitarse por separado.
- **Valores predeterminados**:
  - `Alpha` = 0.07
  - `Candle Type` = marco temporal de 1 minuto
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Adaptive Cyber Cycle
  - Stops: Opcional
  - Complejidad: Moderado
  - Marco temporal: Intradía
