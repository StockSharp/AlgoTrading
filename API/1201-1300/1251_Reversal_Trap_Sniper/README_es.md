# Estrategia Sniper de Trampa de Reversión
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Reversal Trap Sniper busca trampas de RSI donde el impulso se reinicia pero el precio sigue moviéndose.
Compra después de una reversión en sobrecompra que aun así cierra más alto, y vende después de una reversión en sobreventa que aun así cierra más bajo.

## Detalles

- **Criterios de entrada**: RSI en sobrecompra/sobreventa hace tres barras con el RSI actual cruzando de vuelta y el precio continuando en la misma dirección
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop ATR, objetivo o máximo de barras
- **Stops**: Basado en ATR
- **Valores predeterminados**:
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `RiskReward` = 2
  - `MaxBars` = 30
  - `AtrLength` = 14
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: RSI, ATR
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
