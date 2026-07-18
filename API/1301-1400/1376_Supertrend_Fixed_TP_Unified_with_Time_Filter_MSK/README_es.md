# Estrategia Supertrend con TP Fijo Unificado y Filtro de Tiempo MSK
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el indicador Supertrend con toma de ganancias porcentual fija, filtro de precio opcional y filtro de tiempo en zona horaria de Moscú.

## Detalles
- **Criterios de entrada**: Cambio de dirección de Supertrend con filtros opcionales de precio y tiempo
- **Largo/Corto**: Configurable (largo, corto o ambos)
- **Criterios de salida**: Toma de ganancias fija o señal opuesta
- **Stops**: Solo toma de ganancias
- **Valores predeterminados**:
  - `AtrPeriod` = 23
  - `Factor` = 1.8m
  - `TakeProfitPercent` = 1.5m
  - `PriceFilter` = 10000m
  - `TimeFrom` = 0
  - `TimeTo` = 23
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Supertrend
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
