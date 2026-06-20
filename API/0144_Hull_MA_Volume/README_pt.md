# Hull Ma Volume Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Estratégia que utiliza a Média Móvel Hull para a direção da tendência e a confirmação de volume para entradas de operações.

Os testes indicam um retorno anual médio de aproximadamente 169%. Funciona melhor no mercado de criptomoedas.

A média móvel Hull suaviza o ruído e o aumento de volume confirma a convicção. As entradas ocorrem quando o preço se move com a inclinação Hull apoiado por um aumento de volume.

Este método é voltado para traders que observam forte participação em rompimentos. Stops baseados em ATR protegem contra reversões repentinas.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `HullMA(t) > HullMA(t-1) && Volume > AvgVolume * VolumeMultiplier`
  - Vendido: `HullMA(t) < HullMA(t-1) && Volume > AvgVolume * VolumeMultiplier`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Comprado: `HullMA(t) < HullMA(t-1)`
  - Vendido: `HullMA(t) > HullMA(t-1)`
- **Stops**: `StopLossAtr` ATR a partir da entrada
- **Valores padrão**:
  - `HullPeriod` = 9
  - `VolumePeriod` = 20
  - `VolumeMultiplier` = 1.5m
  - `StopLossAtr` = 2.0m
  - `AtrPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Hull MA, Moving Average, Volume
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

