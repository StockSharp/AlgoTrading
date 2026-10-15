# Estrategia de Ruptura de Rango ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia entra en posiciones largas cuando el cierre supera el cierre más alto de un período de retrospectiva mientras el ADX permanece por debajo de un umbral especificado, lo que indica un mercado en calma. Las operaciones se limitan a una sesión definida y a un número máximo de operaciones por día. Un stop-loss fijo en unidades de precio protege cada posición.

## Detalles

- **Criterios de entrada**: `Close >= previous highest close` y `ADX < threshold` dentro de la sesión
- **Largo/Corto**: Solo largos
- **Criterios de salida**: Stop-loss o fin de sesión
- **Stops**: Sí
- **Valores predeterminados**:
  - `AdxPeriod` = 14
  - `HighestPeriod` = 34
  - `AdxThreshold` = 17.5
  - `StopLoss` = 1000
  - `MaxTradesPerDay` = 3
  - `CandleType` = TimeSpan.FromMinutes(30)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Solo largos
  - Indicadores: ADX
  - Stops: Sí
  - Complejidad: Principiante
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
