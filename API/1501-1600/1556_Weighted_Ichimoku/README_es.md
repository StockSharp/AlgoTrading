# Estrategia Ichimoku Ponderada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia combina señales de Ichimoku en una puntuación ponderada.
Compra cuando la puntuación supera el umbral de compra y sale cuando la puntuación cae por debajo del umbral de venta.

## Detalles

- **Criterios de entrada**: puntuación >= BuyThreshold
- **Largo/Corto**: Solo largos
- **Criterios de salida**: puntuación <= SellThreshold o por debajo de cero si el umbral está desactivado
- **Stops**: No
- **Valores predeterminados**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `Offset` = 26
  - `BuyThreshold` = 60
  - `SellThreshold` = -49
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Largo
  - Indicadores: Ichimoku
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
