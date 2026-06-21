# Estrategia Color Zerolag JCCX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia inspirada en el indicador ColorZerolagJCCX de MetaTrader. Aproxima el oscilador original utilizando dos medias móviles simples.
La estrategia va largo cuando la media rápida cruza por debajo de la media lenta y va corto cuando la media rápida cruza por encima de la media lenta.

## Detalles

- **Criterios de entrada**:
  - Largo: `La MA rápida cruza por debajo de la MA lenta`
  - Corto: `La MA rápida cruza por encima de la MA lenta`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal opuesta
- **Stops**: `StartProtection()`
- **Valores predeterminados**:
  - `FastPeriod` = 8
  - `SlowPeriod` = 21
  - `CandleType` = velas de 4 horas
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Media móvil
  - Stops: Opcional
  - Complejidad: Básico
  - Marco temporal: Swing
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
