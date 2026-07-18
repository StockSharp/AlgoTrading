# Estrategia AI Supertrend Pivot Percentile
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina dos indicadores Supertrend con un filtro ADX y un filtro de percentil de pivote de Williams %R. Se abre una posición larga cuando el precio está por encima de ambos Supertrends, el ADX confirma una tendencia fuerte y el Williams %R está por encima de -50. Las posiciones cortas utilizan las condiciones opuestas.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Precio por encima de ambos Supertrends, ADX > umbral, Williams %R > -50.
  - **Corto**: Precio por debajo de ambos Supertrends, ADX > umbral, Williams %R < -50.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Señal opuesta.
- **Stops**: Take-profit y stop-loss basados en porcentaje.
- **Valores predeterminados**:
  - `Length1` = 10
  - `Factor1` = 3
  - `Length2` = 20
  - `Factor2` = 4
  - `AdxLength` = 14
  - `AdxThreshold` = 20
  - `PivotLength` = 14
  - `TpPercent` = 2
  - `SlPercent` = 1
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: SuperTrend, ADX, Williams %R
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
