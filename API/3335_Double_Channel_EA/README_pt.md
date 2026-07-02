# Estratégia de canal duplo EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

O **Double Channel EA** replica a lógica de negociação do MetaTrader 4 consultor especialista "DoubleChannelEA_v1.2". O StockSharpp
ort adapta o indicador *iDoubleChannel_v1.5* personalizado e executa negociações de breakout quando o indicador imprime setas. A estratégia
y foi projetado para testes discricionários com gerenciamento de risco configurável e filtros de cronograma.

Características principais:

- O `DoubleChannelIndicator` personalizado reconstrói os buffers dos canais superior, inferior e intermediário, além dos sinais de seta de compra/venda.
- Uso de API de alto nível com assinaturas de velas, validação de spread de nível um e auxiliares de pedidos nativos.
- Ferramentas opcionais de gerenciamento de dinheiro: empilhamento de posições, ponto de equilíbrio, trailing stop, take-profit e lógica de stop-loss.
- Filtro de hora do dia e filtro de propagação bloqueiam entradas fora das condições operacionais definidas pelo usuário.

## Lógica de negociação

1. Assine o `CandleType` selecionado e insira cada vela finalizada no `DoubleChannelIndicator`.
2. O indicador armazena uma janela móvel de `ChannelPeriod` velas e calcula:
   - Linha média: média aritmética dos fechamentos.
   - Linha superior: meio mais a diferença de dois envelopes de preços derivados de máximos e mínimos.
   - Linha inferior: meio mais a diferença de envelopes complementares derivados de aberturas e mínimos.
   - Sinais de seta: as duas posições anteriores do canal devem virar e a vela anterior deve fechar na direção do rompimento
kout. As regras correspondem às condições do buffer MT4.
3. Os sinais podem ser atrasados em `IndicatorShift` barras para reproduzir o parâmetro de mudança do indicador.
4. Um sinal de compra abre uma posição longa (empilhamento permitido quando `OpenEverySignal = true`). Um sinal de venda abre uma posição curta. Op.
posições positivas podem ser fechadas imediatamente quando `CloseInSignal = true`.
5. As saídas de proteção gerenciam a posição ativa em cada vela finalizada:
   - Distâncias estáticas de stop-loss/take-profit expressas em unidades de preço absoluto.
   - Ativação do ponto de equilíbrio assim que o preço avançar em `BreakEvenPoints + BreakEvenAfterPoints`.
   - Trailing stop que requer uma melhoria de `TrailingStepPoints` antes da atualização.
6. As inscrições serão rejeitadas quando:
   - A estratégia está fora do horário de negociação (`UseTimeFilter`).
   - O spread ao vivo excede `MaxSpreadPoints`.
   - `MaxOrders` posições empilhadas já estão abertas para a direção atual.

## Gestão de capital

O volume do pedido é calculado como:

```
volume = ManualLotSize * (AutoLotSize ? max(RiskFactor, 0,1) : 1)
```

Ao reverter, a estratégia inclui automaticamente a posição oposta absoluta para mudar para a nova direção em um único movimento.
ordem de ceto.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `CandleType` | Período de 15 minutos | Assinatura de vela primária. |
| `ChannelPeriod` | 14 | Lookback do canal personalizado. |
| `IndicatorShift` | 0 | Atraso antes de agir nos valores do indicador. |
| `OpenEverySignal` | verdade | Permite empilhar posições em sinais consecutivos. |
| `CloseInSignal` | falso | Fecha a posição atual quando uma seta oposta aparece. |
| `UseTakeProfit` | falso | Ativa `TakeProfitPoints`. |
| `TakeProfitPoints` | 10 | Distância absoluta do preço para o alvo. |
| `UseStopLoss` | falso | Ativa `StopLossPoints`. |
| `StopLossPoints` | 10 | Distância absoluta de preço para o stop protetor. |
| `UseTrailingStop` | falso | Ativa a lógica final com `TrailingStopPoints` e `TrailingStepPoints`. |
| `TrailingStopPoints` | 5 | Distância do preço atual até o trailing stop. |
| `TrailingStepPoints` | 1 | Melhoria mínima necessária antes de atualizar o trailing stop. |
| `UseBreakEven` | falso | Permite ajustes de ponto de equilíbrio. |
| `BreakEvenPoints` | 4 | Nível de stop alvo assim que o ponto de equilíbrio for ativado. |
| `BreakEvenAfterPoints` | 2 | Lucro extra necessário antes de ativar o ponto de equilíbrio. |
| `AutoLotSize` | verdade | Multiplica o lote manual por `RiskFactor`. |
| `RiskFactor` | 1 | Multiplicador de risco aplicado ao dimensionamento automático. |
| `ManualLotSize` | 0,01 | Volume base quando o dimensionamento automático está desativado. |
| `UseTimeFilter` | falso | Habilita o filtro de agendamento. |
| `TimeStartTrade` | 0 | Hora de início da negociação (inclusive). |
| `TimeEndTrade` | 0 | Horário final de negociação (exclusivo). Início e fim iguais significam nenhuma restrição. |
| `MaxOrders` | 0 | Máximo de posições empilhadas por direção (0 = ilimitado). |
| `MaxSpreadPoints` | 0 | Spread máximo permitido de compra e venda em unidades de preço. |

## Notas sobre conversão

- O indicador original renderizou setas mudando os valores uma barra à frente. A versão StockSharp armazena snapshots anteriores e
verifica os mesmos critérios de cruzamento antes de emitir um sinal na vela atual.
- A filtragem de propagação depende de dados de nível um. Quando as cotações não estão disponíveis, a estratégia bloqueia novos pedidos, imitando a experiência MQL
rt que se recusou a negociar sem divulgar informações.
- A gestão de dinheiro no MT4 utilizou cálculos baseados em contas. Para portabilidade, a fórmula do volume foi simplificada para um multiplicador de risco
é aplicado ao tamanho do lote manual.
- As distâncias de stop-loss, take-profit, trailing stop e ponto de equilíbrio são interpretadas em unidades de preço absoluto (a mesma convenção que um
são outras StockSharp conversões). Ajuste-os de acordo com o tamanho do tick do instrumento.
