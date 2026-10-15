# Estrategia RMI Trend Sync
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

RMI Trend Sync combina señales de momentum de RSI y MFI con un stop trailing de SuperTrend. Se abre una operación larga cuando el momentum promedio cruza por encima de un umbral con pendiente ascendente de la EMA, mientras que una operación corta se activa en una ruptura descendente. SuperTrend proporciona el trail de salida.

## Detalles

- **Criterios de entrada**: El promedio de momentum cruza umbrales con confirmación de la pendiente de la EMA.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Momentum opuesto o stop de SuperTrend.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `RmiLength` = 21
  - `PositiveThreshold` = 70
  - `NegativeThreshold` = 30
  - `SuperTrendLength` = 10
  - `SuperTrendMultiplier` = 3.5
  - `Direction` = TradeDirection.Both
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: RSI, MFI, EMA, SuperTrend
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
