# Estrategia SuperTrend AI con Oscilador
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

SuperTrend AI Oscillator combina un stop trailing de SuperTrend con un filtro de oscilador personalizado.
La estrategia opera en reversiones de SuperTrend confirmadas por el oscilador.
Las posiciones se gestionan con un stop trailing y un objetivo opcional de relación riesgo-recompensa.

## Detalles

- **Criterios de entrada**: Giro de SuperTrend con oscilador > 50 para largo o < 50 para corto
- **Largo/Corto**: Ambos
- **Criterios de salida**: Stop trailing o take profit de relación riesgo-recompensa
- **Stops**: Trailing
- **Valores predeterminados**:
  - `AtrLength` = 10
  - `Factor` = 1
  - `RiskReward` = 2
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: ATR, Stochastic
  - Stops: Trailing
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
