# Estratégia de Aleatoriedade RRS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia de Aleatoriedade RRS** é uma versão StockSharp da “RRS Aleatoriedade na Natureza EA” para MetaTrader 4.
Ele emula o consultor especialista original, gerando entradas aleatórias de compra ou venda no mercado, aplica níveis de stop-loss e take-profit, opcionalmente rastreia negociações lucrativas e executa liquidação baseada em risco quando as perdas flutuantes excedem o limite configurado.

Como StockSharp usa posições líquidas por título, a exposição simultânea longa e curta não é suportada. O modo "DoubleSide", portanto, alterna a direção de entrada em cada oportunidade, em vez de manter duas negociações cobertas como em MetaTrader.

## Lógica de negociação

1. Em cada vela finalizada, a estratégia avalia o último preço de mercado obtido em negociações ou cotações de Nível 1.
2. Se houver uma posição aberta, ele impõe regras de stop-loss, take-profit e trailing-stop e realiza uma verificação de risco do portfólio.
3. Quando estável, valida as restrições de spread e volume antes de abrir uma nova negociação:
   - O modo **DoubleSide** alterna entre entradas longas e curtas.
   - O modo **OneSide** segue a regra original EA: um número inteiro aleatório em `[0,5]` abre posições compradas para valores `1` ou `4` e posições vendidas para `0` ou `3`.
4. Os volumes de negociação são desenhados uniformemente entre o mínimo e o máximo configurados e alinhados ao passo de volume do instrumento.

## Parâmetros

| Grupo | Nome | Descrição |
|-------|------|-------------|
| Geral | `Mode` | Modo de negociação: entradas alternativas (`DoubleSide`) ou entradas aleatórias fechadas (`OneSide`). |
| Configurações de lote | `MinVolume` / `MaxVolume` | Faixa de volume para negociações geradas aleatoriamente. |
| Proteção | `TakeProfitPoints` | Distância de lucro em etapas de preço. |
| Proteção | `StopLossPoints` | Distância de stop-loss em etapas de preço. |
| Proteção | `TrailingStartPoints` | Distância de lucro que permite o gerenciamento de trailing stop. |
| Proteção | `TrailingGapPoints` | Compensação entre o preço de mercado e o trailing stop. |
| Filtros | `MaxSpreadPoints` | Spread máximo permitido (em etapas de preço) para abertura de novas posições. |
| Filtros | `SlippagePoints` | Configuração de deslizamento informativo (não aplicada automaticamente). |
| Gestão de Risco | `MoneyRiskMode` | Escolha entre perda fixa de moeda ou porcentagem do valor do portfólio. |
| Gestão de Risco | `RiskValue` | Quantidade de risco (moeda ou porcentagem dependendo da modalidade). |
| Geral | `TradeComment` | Comentário informativo anexado aos pedidos gerados. |
| Geral | `CandleType` | Série de velas conduzindo o ciclo de decisão. |

## Notas

- A estratégia depende de assinaturas de dados de mercado para velas, cotações e negociações de Nível 1. Certifique-se de que o tipo de dados selecionado esteja disponível para a segurança escolhida.
- A lógica de trailing stop reflete a implementação MQL: ela é ativada após o preço ganhar `TrailingStartPoints + TrailingGapPoints` passos e então segue o preço a uma distância de `TrailingGapPoints`.
- A gestão de risco compara o PnL flutuante com o limite de perda configurado e liquida a posição quando o limite é violado.
