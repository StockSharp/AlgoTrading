# Estrategia de Cruce X2MA JFatl
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una adaptación de StockSharp del experto MetaTrader `Exp_X2MA_JFatl`. Combina una Media Móvil Simple (SMA) rápida con una Media Móvil Jurik (JMA) lenta y un filtro JMA adicional para confirmar la dirección de la tendencia. Las operaciones se abren cuando la media rápida cruza la lenta y el precio está del mismo lado del filtro. Las posiciones se cierran cuando el precio se mueve contra el filtro o se produce un cruce opuesto.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `SMA_fast` cruza por encima de `JMA_slow` y `Close` > `JMA_filter`.
  - **Corto**: `SMA_fast` cruza por debajo de `JMA_slow` y `Close` < `JMA_filter`.
- **Criterios de salida**:
  - El precio se mueve al lado opuesto del filtro.
  - Cruce opuesto de las medias.
- **Largo/Corto**: Ambos lados.
- **Stops**: No utilizados por defecto.
- **Valores predeterminados**:
  - `Fast MA Length` = 5.
  - `Slow MA Length` = 12.
  - `Filter Length` = 20.
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Múltiples (SMA, JMA)
  - Stops: No
  - Complejidad: Moderado
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
