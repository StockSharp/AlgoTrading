# Estratégia Russian20 Momentum MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Russian20 Momentum MA** é uma conversão direta do consultor especialista MetaTrader 5 `Russian20-hp1.mq5`. O script original foi publicado pela Gordago Software Corp. e baseia-se em um gráfico de duas horas, uma média móvel simples (SMA) de 20 períodos e um indicador Momentum de 5 períodos para identificar continuações de tendências de curto prazo. A implementação no StockSharp mantém o mesmo núcleo analítico enquanto adapta o gerenciamento de ordens e o gerenciamento de dinheiro à API de estratégia de alto nível.

## Lógica de negociação
- **Frequência de dados:** Trabalha com o tipo de candle definido pelo usuário (o padrão são candles de 2 horas, correspondendo ao período MQL5 `PERIOD_H2`). A lógica é executada apenas quando um candle é fechado.
- **Indicadores:**
  - Média móvel simples com período configurável (padrão 20).
  - Indicador Momentum com período configurável (padrão 5). O nível neutro de Momentum é 100, espelhando a saída padrão do MQL5.
- **Entrada comprada:** Ativada quando todas as seguintes condições são satisfeitas no último candle fechado:
  1. O preço de fechamento está acima da SMA.
  2. O valor de Momentum é maior que 100 (aceleração positiva).
  3. O preço de fechamento é mais alto que o fechamento do candle anterior, garantindo momentum ascendente na ação do preço.
- **Entrada vendida:** Ativada quando todas as seguintes condições são satisfeitas:
  1. O preço de fechamento está abaixo da SMA.
  2. O valor de Momentum é menor que 100 (aceleração negativa).
  3. O preço de fechamento é mais baixo que o fechamento do candle anterior.
- **Saída comprada:** A estratégia liquida posições compradas quando o Momentum cai abaixo de 100 ou quando um limiar de stop-loss ou take-profit de proteção é cruzado.
- **Saída vendida:** A estratégia liquida posições vendidas quando o Momentum sobe acima de 100 ou quando os limiares de proteção configurados são atingidos.

## Gestão de risco
O consultor especialista MQL5 original coloca ordens fixas de stop loss e take profit em "pips" ajustados para preços Forex de 4 e 5 dígitos. A conversão em C# reproduz esse comportamento:
- Calculando um tamanho de pip ajustado a partir do `PriceStep` do instrumento. Para símbolos com três ou cinco casas decimais, o tamanho do pip equivale a `PriceStep * 10`, caso contrário equivale a `PriceStep`.
- Traduzindo as entradas do usuário para stop loss e take profit em distâncias de preço absolutas.
- Monitorando a ação do preço em cada candle fechado e fechando a posição quando o preço cruza os limiares calculados.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `CandleType` | Candles de 2 horas | Tipo de dados usado para geração de sinais. |
| `MovingAverageLength` | 20 | Retrospectiva para o filtro SMA. |
| `MomentumPeriod` | 5 | Retrospectiva para o indicador Momentum. |
| `StopLossBuyPips` | 50 | Distância de stop-loss comprado expressa em pips. Definir 0 para desabilitar. |
| `TakeProfitBuyPips` | 50 | Distância de take-profit comprado em pips. Definir 0 para desabilitar. |
| `StopLossSellPips` | 50 | Distância de stop-loss vendido em pips. Definir 0 para desabilitar. |
| `TakeProfitSellPips` | 50 | Distância de take-profit vendido em pips. Definir 0 para desabilitar. |

Todos os parâmetros numéricos são expostos através de `StrategyParam<T>` e marcados como otimizáveis quando aplicável, permitindo backtesting e otimização com ferramentas StockSharp.

## Notas de implementação
- A estratégia usa a API de alto nível `SubscribeCandles().Bind(...)` para transmitir dados de candles e obter simultaneamente valores de SMA e Momentum sem gerenciamento manual de indicadores.
- Os níveis de Momentum são avaliados exatamente como no script MQL5 (100 como nível neutro). Qualquer violação além dos offsets de stop-loss/take-profit aciona uma saída de mercado, imitando fielmente a lógica original de colocação de ordens.
- O fechamento anterior é armazenado em cache para verificar o momentum do preço sem recorrer a pesquisas em coleções históricas, de acordo com as diretrizes de desempenho do projeto.
- Os ganchos de visualização (`DrawCandles`, `DrawIndicator`, `DrawOwnTrades`) estão conectados por conveniência quando o ambiente host suporta gráficos.

## Dicas de uso
- O período e os parâmetros padrão correspondem à configuração original do autor. Ajuste o tipo de candle ao trabalhar com instrumentos que não produzem barras de 2 horas.
- Ao negociar ativos cotados com tamanhos de tick não convencionais, revise o tamanho de pip calculado para garantir que as distâncias de stop-loss e take-profit permaneçam realistas.
- A estratégia é projetada para uma única posição aberta de cada vez. Negociações manuais externas ou posições simultâneas no mesmo instrumento podem interferir com a lógica de saída integrada.
