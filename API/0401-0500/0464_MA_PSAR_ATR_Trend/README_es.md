# Estrategia de Tendencia MA PSAR ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Tendencia MA PSAR ATR combina un cruce de medias móviles con un filtro Parabolic SAR diario. Las operaciones se realizan solo cuando el precio se alinea por encima o por debajo de ambas medias y el PSAR coincide. Un stop basado en ATR controla el riesgo.

El método es adecuado para traders que buscan seguimiento de tendencia con stops dinámicos. Las señales se activan en velas de 5 minutos por defecto.

## Detalles
- **Criterios de entrada**:
  - **Largo**: MA rápida > MA lenta, Cierre > MA rápida, Mínimo > PSAR diario
  - **Corto**: MA rápida < MA lenta, Cierre < MA rápida, Máximo < PSAR diario
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: La tendencia se vuelve bajista o el precio cae por debajo del stop ATR
  - **Corto**: La tendencia se vuelve alcista o el precio sube por encima del stop ATR
- **Stops**: Sí, basado en ATR.
- **Valores predeterminados**:
  - `FastMaPeriod` = 40
  - `SlowMaPeriod` = 160
  - `SarStep` = 0.02m
  - `SarMaxStep` = 0.2m
  - `AtrPeriod` = 14
  - `AtrMultiplierLong` = 2m
  - `AtrMultiplierShort` = 2m
  - `UsePsarFilter` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: MA, Parabolic SAR, ATR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
