# Estratégia Straddle News
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia projetada para divulgações de notícias de alta volatilidade. Coloca ordens stop simétricas em ambos os lados do preço atual para capturar rompimentos. Quando uma ordem é acionada, a ordem pendente oposta é cancelada e um trailing stop protege a posição aberta.

## Detalhes

- **Critérios de entrada**: aguardar spread abaixo de `SpreadOperation`, então colocar compra stop em Ask + `PipsAway` pontos e venda stop em Bid - `PipsAway` pontos
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop loss ou take profit de proteção, ou trailing stop quando o preço recua `TrailingStop` pontos
- **Stops**: Stop loss e take profit iniciais via `StartProtection`; trailing stop personalizado no código
- **Valores padrão**:
  - `StopLoss` = 100
  - `TakeProfit` = 300
  - `TrailingStop` = 50
  - `PipsAway` = 50
  - `BalanceUsed` = 0.01
  - `SpreadOperation` = 25
  - `Leverage` = 400
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Básico
  - Período: Level1 / Tick
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto

## Como funciona

1. Assinar cotações Level1 para acessar os preços de compra e venda atuais.
2. Quando o spread for pequeno o suficiente, calcular o volume usando o valor do portfólio, alavancagem e `BalanceUsed`.
3. Colocar ordens pendentes de compra e venda stop com offsets definidos por `PipsAway`.
4. Quando uma posição for aberta, cancelar a ordem pendente oposta.
5. Anexar ordens de stop loss e take profit baseadas em `StopLoss` e `TakeProfit`.
6. Rastrear o preço mais alto/mais baixo desde a entrada e sair se o preço recuar mais de `TrailingStop` pontos.
