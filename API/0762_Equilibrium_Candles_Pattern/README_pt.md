# Estratégia de Padrão de Velas de Equilíbrio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que utiliza velas de equilíbrio para detectar tendências curtas e entrar nos recuos. O equilíbrio é o ponto médio entre os preços mais altos e mais baixos ao longo de uma janela de retrospecto. Após uma sequência de alta ou baixa, um movimento de volta através do equilíbrio aciona uma entrada. O ATR é usado para stop/objetivo opcional e para sair em velas inusualmente grandes.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Após tendência de alta quando o preço cruza abaixo do equilíbrio.
  - **Vendido**: Após tendência de baixa quando o preço cruza acima do equilíbrio.
- **Comprado/Vendido**: Ambos
- **Stops**: Stop loss e take profit baseados em ATR (opcional)
- **Valores padrão**:
  - `EquilibriumLength` = 9
  - `CandlesForTrend` = 7
  - `MaxPullbackCandles` = 2
  - `AtrPeriod` = 14
  - `StopMultiplier` = 2
  - `UseTpSl` = true
  - `UseBigCandleExit` = true
  - `BigCandleMultiplier` = 1
  - `UseReverse` = false
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
