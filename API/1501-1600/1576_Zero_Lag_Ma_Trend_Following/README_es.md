# Seguimiento de tendencia Zero-Lag MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sistema de seguimiento de tendencia que espera a que una MA sin rezago cruce una EMA y luego entra cuando el precio rompe una caja de tamaño ATR. Las posiciones incluyen objetivos basados en relación riesgo-recompensa.

## Detalles

- **Criterios de entrada**: Cruce de MA sin rezago y ruptura de la caja.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Stop o take profit basados en ATR.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `Length` = 34
  - `AtrPeriod` = 14
  - `RiskReward` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: ZLEMA, EMA, ATR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
