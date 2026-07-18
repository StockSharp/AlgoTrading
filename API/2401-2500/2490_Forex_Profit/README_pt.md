# Estratégia Forex Profit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Tradução do consultor especialista do MetaTrader «Forex Profit». A estratégia aguarda o alinhamento de três médias móveis exponenciais e a confirmação do Parabolic SAR antes de entrar em operações no fechamento de cada vela terminada. O risco é controlado por meio de distâncias assimétricas de stop-loss e take-profit, um stop móvel e um bloqueio de lucro adicional baseado em EMA.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `EMA10` acima de `EMA25` e `EMA50`, `EMA10` da barra anterior em ou abaixo de `EMA50`, e Parabolic SAR abaixo do fechamento anterior.
  - Vendido: `EMA10` abaixo de `EMA25` e `EMA50`, `EMA10` da barra anterior em ou acima de `EMA50`, e Parabolic SAR acima do fechamento anterior.
  - Os sinais são avaliados apenas uma vez por vela concluída.
- **Critérios de saída**:
  - Fechar comprado quando `EMA10` vira abaixo de seu valor anterior *e* o lucro atual excede o `ProfitThreshold`.
  - Fechar vendido quando `EMA10` vira acima de seu valor anterior *e* o lucro atual excede o `ProfitThreshold`.
  - Níveis protetores de stop-loss e take-profit definidos na abertura da ordem (distâncias diferentes para comprado vs. vendido).
  - O stop móvel é ativado após o preço se mover `TrailingStopPoints` além da entrada e é atualizado em incrementos de `TrailingStepPoints`.
- **Stops**: Sim — stop-loss fixo, take-profit fixo e gestão de stop móvel.
- **Valores padrão**:
  - `FastEmaLength` = 10
  - `MediumEmaLength` = 25
  - `SlowEmaLength` = 50
  - `TakeProfitBuyPoints` = 55
  - `TakeProfitSellPoints` = 65
  - `StopLossBuyPoints` = 60
  - `StopLossSellPoints` = 85
  - `TrailingStopPoints` = 74
  - `TrailingStepPoints` = 5
  - `ProfitThreshold` = 10
  - `SarAcceleration` = 0.02
  - `SarMaxAcceleration` = 0.2
  - `Volume` = 1
  - `CandleType` = período de 1 hora
- **Notas adicionais**:
  - As distâncias de stop/alvo são expressas em passos de preço do instrumento e convertidas automaticamente usando o tamanho do tick do instrumento.
  - As saídas baseadas em lucro dependem do lucro total da posição (incluindo volume) convertido de ticks de preço para a moeda da conta.
  - A lógica de trailing mantém o stop atrás das oscilações de preço sem ultrapassar o passo configurado.
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado/Vendido
  - Indicadores: EMA, Parabolic SAR
  - Stops: Sim (fixos + trailing)
  - Complexidade: Intermediário
  - Período: Configurável (padrão 1 hora)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
