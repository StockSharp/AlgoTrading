# Estrategia MA2CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de cruce de medias móviles confirmada por CCI. Utiliza ATR para el stop-loss.

## Detalles

- **Criterios de entrada**:
  - Largo cuando la SMA rápida cruza por encima de la SMA lenta y el CCI cruza por encima de 0.
  - Corto cuando la SMA rápida cruza por debajo de la SMA lenta y el CCI cruza por debajo de 0.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Cruce inverso o stop-loss a 1 ATR desde la entrada.
- **Stops**: Stop basado en ATR a precio de entrada ± ATR.
- **Valores predeterminados**:
  - `FastMaPeriod` = 4
  - `SlowMaPeriod` = 8
  - `CciPeriod` = 4
  - `AtrPeriod` = 4
  - `CandleType` = 1 minuto
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: SMA, CCI, ATR
  - Stops: ATR
  - Complejidad: Principiante
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
