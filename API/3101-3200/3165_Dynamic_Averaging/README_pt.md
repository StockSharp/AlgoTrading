# Estratégia de Dynamic Averaging
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Dynamic Averaging é um port direto do assessor especialista do MetaTrader 5 "Dynamic averaging.mq5" (id 23319). A estratégia combina um oscilador Stochastic rápido com um filtro de volatilidade baseado no desvio padrão. As operações são permitidas apenas enquanto a volatilidade do mercado permanece abaixo de sua média móvel, forçando entradas durante consolidações onde as reversões do Stochastic são mais confiáveis.

## Parâmetros
- **TradeVolume** – tamanho da ordem para cada nova entrada. É automaticamente dobrado após uma sequência de perdas e redefinido após uma lucrativa.
- **MinimumProfit** – lucro flutuante (em moeda da conta) que fecha todas as posições abertas quando excedido.
- **SlidingWindowDays** – número de dias calendário usados para calcular a média dos valores de desvio padrão e construir a linha de base de volatilidade.
- **StochasticKPeriod** – número de barras para o cálculo do %K.
- **StochasticDPeriod** – comprimento de suavização para a linha %D.
- **StochasticSlowPeriod** – período de desaceleração final para o oscilador Stochastic.
- **StdDevPeriod** – período de retrovisão para o indicador de desvio padrão.
- **CandleType** – velas fonte para cálculos (padrão: período de 15 minutos).

## Regras de negociação
1. A estratégia opera apenas com velas concluídas. No fechamento de cada barra, os filtros de Stochastic e volatilidade são atualizados via `SubscribeCandles().BindEx`.
2. Calcular a volatilidade do mercado usando `StandardDeviation(StdDevPeriod)` e compará-la com a volatilidade média calculada por `SimpleMovingAverage` sobre as últimas `SlidingWindowDays` barras.
3. Se o desvio padrão atual estiver acima da média móvel, a barra é ignorada.
4. Quando a volatilidade está baixa:
   - Entrar **comprado** se %K estiver abaixo de 25 e a inclinação dos dois últimos valores de %K for positiva (último valor menos o valor há duas barras).
   - Entrar **vendido** se %K estiver acima de 75 e a inclinação dos dois últimos valores de %K for negativa.
5. As posições são revertidas enviando volume suficiente para nivelar o lado oposto mais a nova exposição de `TradeVolume`.
6. Quando o PnL flutuante da posição aberta excede `MinimumProfit`, a estratégia sai imediatamente do mercado.

## Dimensionamento de posição e recuperação
- O tamanho inicial da ordem é igual a `TradeVolume`.
- Após o fechamento da posição, a variação do PnL realizado é inspecionada.
  - Uma **perda** dobra o próximo tamanho de negociação (passo `martingale`) para replicar o comportamento do EA original.
  - **Lucro ou breakeven** redefine o tamanho para o `TradeVolume` base.

## Detalhes de implementação
- Velas, valores de Stochastic e desvio padrão são processados através da API de alto nível com `BindEx`, evitando o gerenciamento manual de buffers.
- A janela deslizante de volatilidade converte dias calendário em contagens de barras usando o período das velas quando disponível.
- O controle de lucro flutuante depende do fechamento da vela atual e `PositionAvgPrice`, correspondendo à implementação MQL que soma apenas o lucro de posição aberta.
- Todos os comentários de código estão em inglês; nenhuma versão em Python é fornecida conforme os requisitos da tarefa.
