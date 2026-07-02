# Estrategia Ultimate Trading Bot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia solo largos que combina cruces de RSI, media móvil, MACD y estocástico para determinar entradas y salidas.

## Detalles

- **Criterios de entrada**: RSI cruza al alza desde la zona de sobreventa mientras el precio está por encima de la MA, MACD y estocástico cruzan al alza.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Condiciones de cruce opuestas.
- **Stops**: Sin stops explícitos.
- **Valores predeterminados**:
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `MaLength` = 50
  - `StochLength` = 14
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Largo
  - Indicadores: RSI, MA, MACD, Stochastic
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Medio
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
