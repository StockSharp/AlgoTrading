# Estrategia de Scalping Inteligente PRO Mejorada v2 para NAS100 y Oro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia hace scalping de movimientos a corto plazo usando EMA9 y VWAP como guías dinámicas, RSI para el momentum y ATR para la gestión del riesgo. Un filtro de tendencia EMA200 de 15 minutos mantiene las operaciones en la dirección de la tendencia predominante, mientras que un filtro de volumen pico busca velas fuertes. El tamaño de posición se calcula por riesgo y se admiten trailing stops opcionales y períodos de enfriamiento entre operaciones.

## Detalles

- **Criterios de entrada**: señal de indicador
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss, take-profit o señal opuesta
- **Stops**: Sí, basados en ATR
- **Valores predeterminados**:
  - `CandleType` = 1 minute
  - `RiskPercent` = 1%
  - `AtrMultiplierSl` = 1
  - `AtrMultiplierTp` = 2
  - `CooldownMins` = 30
  - `StartHour` = 13
  - `EndHour` = 20
- **Filtros**:
  - Categoría: Scalping
  - Dirección: Ambos
  - Indicadores: EMA, VWAP, RSI, ATR, EMA200
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
