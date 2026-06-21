# Estrategia SMC para BTC en 1H con OB y FVG
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en Smart Money Concepts para Bitcoin en velas de 1 hora. El sistema entra largo después de una ruptura alcista de estructura cuando el precio regresa al bloque de órdenes detectado o a la brecha de valor justo. El stop-loss utiliza un multiplicador ATR y el take-profit se calcula a partir de una relación riesgo/beneficio.

## Detalles

- **Criterios de entrada**: Tras BOS alcista, comprar si el precio toca el bloque de órdenes o la brecha de valor justo dentro de `ZoneTimeout` barras.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Take-profit y stop-loss fijos.
- **Stops**: Fijo usando ATR.
- **Valores predeterminados**:
  - `UseOrderBlock` = true
  - `UseFvg` = true
  - `AtrFactor` = 6
  - `RiskRewardRatio` = 2.5
  - `ZoneTimeout` = 10
  - `CandleType` = TimeSpan.FromHours(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Largo
  - Indicadores: ATR
  - Stops: Fijo
  - Complejidad: Simple
  - Marco temporal: Intradía (1H)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
