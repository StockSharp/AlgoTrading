# Filtro de Volumen ZPF
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El Filtro de Volumen ZPF combina dos medias móviles con una media de volumen. El valor del indicador es la diferencia entre la media rápida y la lenta, suavizada por el volumen. Cuando este valor cruza por encima de cero, se asume presión alcista; un cruce por debajo de cero señala presión bajista.

La estrategia opera en ambas direcciones. Las entradas ocurren cuando el indicador ZPF cruza la línea de cero. Las posiciones se cierran cuando ocurre un cruce opuesto.

## Detalles

- **Criterios de entrada**: ZPF cruza por encima o por debajo de cero.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Cruce opuesto de la línea de cero.
- **Stops**: No.
- **Valores predeterminados**:
  - `Length` = 12
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Moving Average, Volume
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Swing
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

