# EMA RSI Estratégia de cruzamento adaptativo de volatilidade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma porta direta do consultor especialista MetaTrader **EA_MARSI_1-02**. Ele comercializa crossovers entre duas cópias de
Indicador *EMA_RSI_VA* personalizado do número inteiro, uma média móvel adaptável à volatilidade impulsionada pelo Índice de Força Relativa (RSI).
Sempre que a linha lenta cruza a linha rápida, o motor inverte a posição líquida, reproduzindo o "flip on crossover" original
comportamento, respeitando as práticas recomendadas de tratamento de pedidos de StockSharp.

## Mecânica do indicador

O pacote MQL original vem com um indicador personalizado chamado `EMA_RSI_VA`. Ele calcula um EMA suavizado por preço cujo efetivo
o comprimento é modulado pela distância de RSI de seu valor neutro. A porta StockSharp apresenta o
Classe `EmaRsiVolatilityAdaptiveIndicator` que replica a fórmula com precisão:

1. Calcule RSI na fonte `AppliedPrice` selecionada com período `RSIPeriod`.
2. Meça a distância RSI de 50 (`|RSI - 50| + 1`), que atua como um proxy de volatilidade.
3. Derive um multiplicador adaptativo
`multi = (5 + 100 / RSIPeriod) / (0.06 + 0.92 * dist + 0.02 * dist^2)`.
4. Multiplique o período EMA configurado por este multiplicador para obter um comprimento dinâmico `pdsx`.
5. Aplique a recursão padrão EMA com fator de suavização `2 / (pdsx + 1)` usando o preço aplicado da vela como entrada.

Grandes excursões RSI encurtam a janela de suavização e fazem a linha reagir mais rapidamente; um plano RSI alonga a janela e umedece
barulho. Ambas as linhas lenta e rápida expõem o conjunto completo de modos de preço suportados por `StockSharp.Messages.AppliedPrice`.

## Regras de negociação

- **Detecção de sinal**
  - *Venda / venda a descoberto*: anterior lento < anterior rápido **e** atual lento ≥ atual rápido.
  - *Comprar / longo*: lento anterior > rápido anterior **e** lento atual ≤ rápido atual.
- **Execução**
  - A estratégia analisa apenas velas finalizadas da série de velas configuradas.
  - Quando ocorre um sinal, ele envia uma ordem de mercado dimensionada para fechar a exposição existente e abrir a nova direção.
  - Os limites de troca são respeitados por meio de `Security.MinVolume`, `Security.VolumeStep` e `Security.MaxVolume`.
- **Reversões**
  - Os pedidos são compensados de modo que uma única chamada `SellMarket` ou `BuyMarket` assuma a posição na linha zero, correspondendo ao
Comportamento MQL em que um sinal oposto inverte imediatamente a negociação.

## Gestão de risco

- `TakeProfitPoints` e `StopLossPoints` replicam os campos TP/SL do consultor especialista (expressos em faixas de preço). Quando
o valor for diferente de zero, a estratégia inicia o gerenciador de proteção de StockSharp com compensações de preço absoluto e `useMarketOrders = true`
para espelhar o loop de modificação de parada/limite `OrderSend` original.
- `UseBalanceMultiplier` implementa a alternância `use_Multpl`. Quando ativo, o volume efetivo do pedido torna-se
`Volume * PortfolioEquity / MaxDrawdown` com uma braçadeira defensiva para troca de restrições.
- A chamada da classe base `StartProtection()` ainda é executada para que os módulos de risco externos possam anexar o trailing ou o ponto de equilíbrio
lógica, se necessário.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `Volume` | `0.1` | Tamanho base da ordem de mercado antes de qualquer multiplicador de saldo ser aplicado. |
| `TakeProfitPoints` | `0` | Distância de take-profit em pontos de instrumento; `0` desativa a perna de lucro. |
| `StopLossPoints` | `0` | Distância de stop-loss em pontos de instrumento; `0` desativa a parada protetora. |
| `UseBalanceMultiplier` | `false` | Ativa o dimensionamento de posição proporcional ao equilíbrio idêntico a `use_Multpl` no EA. |
| `MaxDrawdown` | `10000` | Denominador do multiplicador de saldo; corresponde ao EA do `Max_drawdown`. |
| `SlowRsiPeriod` | `310` | RSI lookback para a linha EMA_RSI_VA lenta. |
| `SlowEmaPeriod` | `40` | Comprimento base de EMA para a linha lenta antes da adaptação de RSI. |
| `SlowAppliedPrice` | `Close` | Modo de preço encaminhado para o indicador lento. |
| `FastRsiPeriod` | `200` | RSI lookback para a linha rápida EMA_RSI_VA. |
| `FastEmaPeriod` | `50` | Comprimento base de EMA para a linha rápida antes da adaptação de RSI. |
| `FastAppliedPrice` | `Close` | Modo de preço encaminhado para o indicador rápido. |
| `CandleType` | `TimeFrame(1m)` | Série de velas usadas para cálculos. |

## Notas de implementação

- A porta é escrita com StockSharp de alto nível API (`SubscribeCandles().Bind(...)`) para evitar loops manuais de indicadores.
- Apenas velas concluídas são processadas, correspondendo às chamadas `CopyBuffer(..., 1, 2, ...)` na origem MQL.
- A normalização de volume usa `Security.MinVolume`, `Security.VolumeStep` e `Security.MaxVolume`, evitando pedidos inválidos em
verdadeiras trocas.
- Uma versão Python é omitida intencionalmente conforme solicitado; o diretório contém apenas a implementação e documentação do C#.

O comportamento resultante espelha a fonte EA enquanto expõe parâmetros amigáveis e controles de risco StockSharp adequados para
Designer, Runner ou qualquer host personalizado criado no StockSharp API.
