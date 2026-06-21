# Estrategia de Oscilador Reflex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza el Reflex Oscillator de John Ehlers. Entra largo cuando el oscilador cruza por encima de un umbral superior y entra corto cuando cruza por debajo de un umbral inferior. Las posiciones se cierran cuando el oscilador regresa a la línea cero.

## Detalles

- **Criterios de entrada**:
  - **Largo**: el oscilador cruza por encima de `UpperLevel`.
  - **Corto**: el oscilador cruza por debajo de `LowerLevel`.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Posición larga: el oscilador cruza por debajo de cero.
  - Posición corta: el oscilador cruza por encima de cero.
- **Stops**: No.
- **Valores predeterminados**:
  - `ReflexPeriod` = 20.
  - `SuperSmootherPeriod` = 8.
  - `PostSmoothPeriod` = 33.
  - `UpperLevel` = 1.
  - `LowerLevel` = -1.
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: Único
  - Stops: No
  - Complejidad: Avanzado
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
