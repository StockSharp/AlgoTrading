# Estrategia Agresiva de Alta IV
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia Agresiva de Alta IV combina cruces de EMA con un filtro de volatilidad ATR. Las operaciones se abren solo cuando la volatilidad supera su media en una desviación estándar y se cierran con objetivos basados en ATR.

Las pruebas indican retornos sólidos en mercados de alta volatilidad.

La estrategia entra en cruces de EMA durante períodos de volatilidad elevada, buscando ganancias rápidas con controles de riesgo predefinidos.

Las posiciones se cierran utilizando niveles de stop-loss y take-profit basados en ATR.

## Detalles

- **Criterios de entrada**: EMA rápida cruza EMA lenta con ATR por encima de su media más la desviación estándar.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Stop-loss o take-profit basado en ATR alcanzado.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `FastEmaLength` = 10
  - `SlowEmaLength` = 30
  - `AtrLength` = 14
  - `AtrMeanLength` = 20
  - `AtrStdLength` = 20
  - `RiskFactor` = 0.01m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: EMA, ATR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto
