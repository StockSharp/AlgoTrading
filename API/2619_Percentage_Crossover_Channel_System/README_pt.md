# Estratégia do Sistema de Canal de Cruzamento Percentual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é um port direto do consultor especialista MetaTrader *Exp_PercentageCrossoverChannel_System*. Ela rastreia como o preço interage com um "Percentage Crossover Channel" personalizado e reage quando os candles voltam ao interior do canal após uma ruptura anterior. O código foi reescrito com as APIs de alto nível do StockSharp e preserva o fluxo de sinais original.

## Lógica de negociação

1. **Construção do indicador**
   - O Percentage Crossover Channel constrói uma linha média adaptativa que permanece próxima ao preço, mas não pode se afastar mais rapidamente do que uma porcentagem fixa (`Percent`).
   - As bandas superior e inferior são derivadas da linha média usando a mesma distância percentual.
   - Cada candle completado recebe cor de acordo com sua relação com o canal de há `Shift` barras:
     - Cor `3` / `4`: fechamento acima da banda superior (corpo de candle de baixa/alta respectivamente).
     - Cor `0` / `1`: fechamento abaixo da banda inferior (corpo de baixa/alta respectivamente).
     - Cor `2`: o candle terminou dentro do canal.

2. **Regras de entrada e saída**
   - Avaliar o último candle do `SignalBar` e o imediatamente anterior (espelha a chamada `CopyBuffer` do MQL).
   - **Sequência de alta** (`olderColor > 2`): o mercado fechou recentemente acima do canal. Se o candle mais recente voltou ao interior (`recentColor < 3`) a estratégia:
     - Fecha qualquer short ativo se `SellPositionsClose` estiver habilitado.
     - Abre uma posição comprada quando não há operações abertas e `BuyPositionsOpen` está habilitado.
   - **Sequência de baixa** (`olderColor < 2`): o mercado fechou recentemente abaixo do canal. Se o último candle retornou ao interior (`recentColor > 1`) a estratégia:
     - Fecha qualquer long se `BuyPositionsClose` estiver habilitado.
     - Abre uma posição vendida quando não há operações ativas e `SellPositionsOpen` está habilitado.
   - A lógica portanto aguarda um rompimento seguido de uma re-entrada no canal antes de se comprometer na direção do rompimento.

3. **Gestão de risco**
   - Stop loss e take profit opcionais são expressos em passos de preço e avaliados em máximas/mínimas de candles.
   - Se uma ordem de proteção for acionada, a estratégia sai do mercado e ignora novas entradas para a mesma barra, imitando o comportamento MQL onde stops do broker fecham a operação primeiro.

## Parâmetros

| Nome | Descrição |
| ---- | ----------- |
| `Percent` | Largura do canal em percentual. Corresponde ao parâmetro de entrada do indicador MQL. |
| `Shift` | Número de barras usadas para comparar o rompimento com as bandas históricas. |
| `SignalBar` | Deslocamento (em barras) para avaliação de sinais. Um valor de 1 significa "barra anterior" como o padrão original do EA. |
| `BuyPositionsOpen` / `SellPositionsOpen` | Habilitar ou desabilitar a abertura de operações na direção correspondente. |
| `BuyPositionsClose` / `SellPositionsClose` | Habilitar ou desabilitar o fechamento forçado de posições opostas em um novo sinal. |
| `StopLoss` | Distância do stop loss expressa em múltiplos de `Security.PriceStep`. Definir como zero para desabilitar. |
| `TakeProfit` | Distância do take-profit em passos de preço. Definir como zero para desabilitar. |
| `CandleType` | Período para assinatura de candles. Padrão são barras de quatro horas para refletir `PERIOD_H4`. |

## Notas de implementação

- A lógica do indicador está implementada inline porque o StockSharp não fornece um Percentage Crossover Channel nativo. Os cálculos da linha média, derivação de bandas e atribuições de cores reproduzem o algoritmo-fonte do MQL passo a passo.
- O gerenciamento de posições segue as funções auxiliares originais (`BuyPositionOpen`, `SellPositionOpen`, etc.) fechando operações opostas antes de abrir uma nova e pulando entradas quando uma posição oposta ainda está presente.
- Gerenciamento de capital, tratamento de desvio e dimensionamento de lotes específico do modo de margem do arquivo include original não são replicados. Os usuários do StockSharp devem configurar o volume da estratégia através das propriedades padrão de `Strategy` ou do ambiente de hospedagem.
- Os valores de stop loss / take profit são interpretados como *passos de preço* porque os parâmetros do MetaTrader são especificados em pontos. Certifique-se de que o instrumento conectado expõe um `PriceStep` válido.

## Dicas de uso

- Conecte a estratégia a um instrumento com dados confiáveis de quatro horas se desejar comportamento idêntico ao MetaTrader. Ajuste `CandleType` para experimentar operação intradiária.
- Como a lógica de entrada requer dois candles completados com informações de cor válidas, permita que a estratégia aqueça com pelo menos `Shift + SignalBar + 1` barras de histórico.
- O canal é sensível ao parâmetro `Percent`. Valores menores ficam próximos ao preço e aumentam a frequência de trading, enquanto valores maiores se concentram em rompimentos mais fortes.
- Ao combinar com controles de risco a nível de portfólio, tenha em mente que esta implementação abre no máximo uma posição por vez e alterna entre estados comprado, neutro ou vendido.
