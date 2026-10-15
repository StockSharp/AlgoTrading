# Estrategia de Centro de Gravedad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el indicador Center of Gravity que multiplica SMA y WMA y suaviza el resultado. Se abre una posición larga cuando la línea central cruza por encima de su media suavizada y se abre una posición corta en el cruce opuesto. Las posiciones se cierran cuando la señal cambia en contra de la dirección actual.

## Detalles

- **Criterios de entrada**: La línea central cruza su media suavizada
- **Largo/Corto**: Ambos
- **Criterios de salida**: La señal cambia de lado
- **Stops**: No
- **Valores predeterminados**:
  - `CandleType` = H4
  - `Period` = 10
  - `SmoothPeriod` = 3
- **Filtros**:
  - Categoría: Indicador
  - Dirección: Ambos
  - Indicadores: SMA, WMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
