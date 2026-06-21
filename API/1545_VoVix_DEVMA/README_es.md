# Estrategia VoVix DEVMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia analiza el comportamiento de la volatilidad utilizando Medias Móviles de Desviación (DEVMA) construidas sobre la desviación estándar del ATR. Opera transiciones entre regímenes de contracción y expansión y utiliza salidas basadas en ATR.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El DEVMA rápido cruza por encima del DEVMA lento.
  - **Corto**: El DEVMA rápido cruza por debajo del DEVMA lento.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Stop-loss y take-profit basados en ATR.
- **Stops**: Sí, múltiplos de ATR.
- **Valores predeterminados**:
  - `DeviationLookback` = 59
  - `FastLength` = 20
  - `SlowLength` = 60
  - `ATR SL Mult` = 2
  - `ATR TP Mult` = 3
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Múltiples
  - Stops: Sí
  - Complejidad: Complejo
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
