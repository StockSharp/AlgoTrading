# Estrategia Multi-Factor
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia Multi-Factor combina MACD, RSI y dos medias móviles para operar con confirmación de tendencia. Las operaciones largas ocurren cuando la línea MACD está por encima de su señal, el RSI está por debajo de 70, el precio está por encima de la SMA de 50 períodos y la SMA de 50 está por encima de la SMA de 200. Las operaciones cortas usan condiciones opuestas.

Los stops y objetivos se basan en múltiplos del ATR.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `MACD > Signal` && `RSI < 70` && `Close > SMA50` && `SMA50 > SMA200`.
  - **Corto**: `MACD < Signal` && `RSI > 30` && `Close < SMA50` && `SMA50 < SMA200`.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Stop loss y take profit basados en ATR.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `RsiLength` = 14
  - `AtrLength` = 14
  - `StopAtrMultiplier` = 2
  - `ProfitAtrMultiplier` = 3
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: MACD, RSI, SMA, ATR
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
