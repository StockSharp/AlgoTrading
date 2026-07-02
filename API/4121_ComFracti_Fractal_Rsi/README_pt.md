# Estratégia ComFracti Fractal RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
ComFracti Fractal RSI Strategy é uma versão StockSharp do especialista MetaTrader *ComFracti*. O algoritmo procura viés direcional usando fractais Bill Williams em dois intervalos de tempo e filtra os sinais com um RSI rápido calculado em velas diárias. Assim que uma configuração válida aparecer, a estratégia abre uma única posição, protege-a com distâncias configuráveis ​​de stop-loss e take-profit e pode opcionalmente sair quando o sinal for revertido ou quando um limite de tempo de retenção for atingido.

A configuração padrão replica o período de negociação de 15 minutos com um período de confirmação de 1 hora e uma duração diária de RSI três períodos usando o preço de abertura da vela, assim como o especialista original.

## Lógica de negociação
1. **Detecção de polarização fractal**
   - As velas finalizadas do período de negociação e do período superior são processadas através de uma janela fractal de cinco barras.
   - Os parâmetros `Primary*Shift` e `Higher*Shift` definem quantas barras a estratégia inspeciona em busca do último fractal confirmado. Os padrões correspondem ao valor original de `3`, o que significa que o código avalia o fractal que foi confirmado há três velas.
   - Um fractal descendente (oscilação baixa) sem um fractal ascendente é tratado como altista (+1). Um fractal ascendente sem um fractal descendente é tratado como baixista (-1).
2. **Filtro diário RSI**
   - Um `RelativeStrengthIndex` com o `RsiPeriod` configurável (padrão `3`) é executado no período diário e usa o preço de abertura da vela, correspondendo à implementação MetaTrader.
   - Configurações longas exigem que RSI esteja abaixo de `50 - RsiBuyOffset`; configurações curtas exigem que RSI esteja acima de `50 + RsiSellOffset`.
3. **Condições de entrada**
   - **Compra**: ambos os rastreadores fractais reportam +1 e o filtro RSI é otimista. A estratégia abre uma posição longa se for plana ou curta, enviando volume suficiente para virar para o lado comprado.
   - **Venda**: Ambos os rastreadores fractais reportam -1 e o filtro RSI é de baixa. A estratégia abre uma posição curta se for plana ou longa, enviando volume suficiente para virar para o lado vendido.
4. **Gerenciamento de posição**
   - Os níveis protetores de stop-loss e take-profit são calculados imediatamente após a mudança de posição com base em `StopLossPips` e `TakeProfitPips` multiplicados pelo tamanho do pip do instrumento.
   - A posição pode ser fechada quando o preço atingir o stop ou alvo, quando `ExpiryMinutes` decorrer ou quando `CloseOnOppositeSignal` estiver ativado e o sinal for revertido.

## Parâmetros
| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `Volume` | Volume de pedidos usado para cada entrada. | `0.1` |
| `TakeProfitPips` | Distância alvo de lucro em pips. Defina como `0` para desativar. | `700` |
| `StopLossPips` | Distância de stop-loss em pips. Defina como `0` para desativar. | `2500` |
| `ExpiryMinutes` | Tempo máximo de espera em minutos antes de forçar uma saída. `0` desativa o cronômetro. | `5555` |
| `CloseOnOppositeSignal` | Feche a posição ativa quando o sinal mudar para a direção oposta. | `false` |
| `PrimaryBuyShift` | As barras voltam para inspecionar o fractal de alta no período de negociação. | `3` |
| `HigherBuyShift` | As barras voltam para inspecionar o fractal de alta no período de tempo mais alto. | `3` |
| `PrimarySellShift` | As barras voltam para inspecionar o fractal de baixa no período de negociação. | `3` |
| `HigherSellShift` | As barras voltam para inspecionar o fractal de baixa no período de tempo mais alto. | `3` |
| `RsiBuyOffset` | Deslocamento abaixo de 50 necessário para configurações longas. | `3` |
| `RsiSellOffset` | Deslocamento acima de 50 necessário para configurações curtas. | `3` |
| `RsiPeriod` | Duração de RSI no período diário. | `3` |
| `CandleType` | Tipo de vela de período de negociação. | Velas de 15 minutos |
| `HigherTimeFrame` | Tipo de vela de prazo de confirmação. | Velas de 1 hora |
| `DailyTimeFrame` | Tipo de vela usado para o diário RSI. | Velas de 1 dia |

## Notas de implementação
- A estratégia utiliza a assinatura de velas de alto nível API (`SubscribeCandles().Bind(...)`) e gerencia indicadores internamente sem expô-los por meio de `Strategy.Indicators`, conforme exigido pelas diretrizes.
- Fractals são calculados por meio de um auxiliar interno que armazena uma janela rolante de cinco velas e atualiza o sinal somente após a confirmação de um fractal.
- Os valores RSI são recuperados via `RelativeStrengthIndex.Process(...)` com o preço de abertura da vela, correspondendo ao modo MetaTrader `PRICE_OPEN`.
- Apenas uma posição é mantida por vez. As ordens de mercado invertem a posição quando necessário, adicionando o volume necessário para cobrir uma exposição existente.
- O tamanho do pip é estimado a partir de `Security.PriceStep` e `Security.Decimals`, usando um multiplicador de 10x para ativos cotados com três ou mais casas decimais, reproduzindo a conversão de MetaTrader `Point` para pip.

## Dicas de uso
- As mudanças fractais devem ser grandes o suficiente para garantir que o índice de vela solicitado exista. Com uma mudança de três, o rastreador requer pelo menos cinco velas finalizadas por período de tempo antes de gerar sinais.
- Ao negociar instrumentos com diferentes tamanhos de tick (por exemplo, índices ou ações), ajuste `TakeProfitPips` e `StopLossPips` para corresponder à definição de pip do instrumento.
- Desativar `CloseOnOppositeSignal` replica o comportamento original do consultor especialista (foi desativado por padrão) e depende apenas de paradas, metas ou do cronômetro de expiração para saídas.
- A estratégia não implementa o Martingale ou o dimensionamento baseado no risco; o cálculo do lote MetaTrader baseou-se em funções de margem da conta que não estão disponíveis em StockSharp. Use o parâmetro `Volume` ou envolva a estratégia em um gerenciador de portfólio se o dimensionamento dinâmico da posição for necessário.
