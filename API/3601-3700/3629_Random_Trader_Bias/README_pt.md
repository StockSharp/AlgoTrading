# Estratégia de Trader de Viés Aleatório
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Random Bias Trader emula o consultor especialista MetaTrader "random trader" usando o StockSharp de alto nível de API.
A cada vela finalizada, a estratégia lança uma moeda virtual e abre uma posição nessa direção quando nenhuma negociação está ativa.
Os níveis de stop-loss e take-profit são derivados de ATR(10) ou de uma distância fixa de pip e dimensionados pela relação recompensa-risco.
O tamanho da posição é calculado a partir da porcentagem de risco configurada e automaticamente limitado pelos limites de volume do instrumento.
Um gatilho de ponto de equilíbrio opcional pode mover o stop loss para o preço de entrada assim que um ganho de pip especificado for atingido.

## Detalhes
- **Dados**: uma assinatura de vela definida por `CandleType`.
- **Critérios de entrada**:
  - Longo: Sem posição aberta, o lançamento da moeda retorna longo. O preço de entrada é igual ao último fechamento.
  - Curto: Sem posição aberta, o lançamento da moeda retorna vendido. O preço de entrada é igual ao último fechamento.
- **Critérios de saída**:
  - Stop-loss: a distância é igual a `LossPipDistance` × tamanho do pip ou `LossAtrMultiplier` × ATR(10) dependendo de `LossType`.
  - Take-profit: Distância de parada multiplicada por `RewardRiskRatio`.
  - Ponto de equilíbrio: Quando ativado, mova o stop para a entrada após o ganho de `BreakevenDistancePips`.
- **Stops**: Stop-loss e take-profit dinâmicos por negociação, stop de equilíbrio opcional.
- **Valores padrão**:
  - `CandleType` = período de 1 minuto
  - `RewardRiskRatio` = 2,0
  - `LossType` = Pip
  - `LossAtrMultiplier` = 5,0
  - `LossPipDistance` = 20 pips
  - `RiskPercentPerTrade` = 1%
  - `UseBreakeven` = Ativado
  - `BreakevenDistancePips` = 10 pips
  - `UseMaxMargin` = Ativado
- **Filtros**:
  - Categoria: Randomizado com tendência neutra
  - Direção: Ambas, determinadas por lance
  - Indicadores: ATR(10) (opcional)
  - Complexidade: Iniciante
  - Nível de risco: Médio, depende do tamanho do stop

## Notas
- Quando o volume baseado no risco se torna demasiado pequeno, a estratégia opcionalmente volta ao volume máximo negociável.
- Os níveis Stop e Target são arredondados para a etapa do preço do instrumento antes de as ordens serem colocadas.
- A lógica do ponto de equilíbrio mantém apenas uma posição aberta por vez, espelhando a lógica original MetaTrader.
