# Estratégia RNN Probability
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia RNN Probability é uma conversão do expert do MetaTrader *RNN (barabashkakvn's edition)*. O robô original coleta três instantâneos RSI separados pelo período RSI e os alimenta a uma rede de probabilidade artesanal que emula uma rede neural recorrente. O port do StockSharp replica este comportamento com a API de assinatura de candles de alto nível, convertendo automaticamente os lotes, passos de preço e distâncias de stop/alvo do MetaTrader em conceitos do StockSharp.

Assim que o valor RSI do último candle finalizado está disponível, a estratégia retrocede um e dois períodos RSI para construir um histórico de três pontos. Essas leituras normalizadas são combinadas com os oito pesos do MetaTrader (`Weight0` … `Weight7`) para produzir uma probabilidade de que o mercado deve cair. A probabilidade é remapeada para o intervalo `[-1; 1]`, e o sinal determina se abre uma posição comprada ou vendida. Apenas uma posição é mantida por vez, correspondendo ao Expert Advisor original.

## Lógica de trading
1. Assinar a série de candles configurada e processar manualmente o indicador `RelativeStrengthIndex` usando a entrada `AppliedPrice` selecionada (abertura por padrão).
2. Armazenar os valores RSI finalizados em um buffer contínuo grande o suficiente para acessar a leitura RSI de um e dois períodos completos atrás.
3. Normalizar os três valores RSI para o intervalo `[0; 1]` e avaliar a rede de probabilidade:
   - O primeiro ramo (`Weight0`, `Weight1`, `Weight2`, `Weight3`) trata o caso quando o RSI atual está na metade inferior (abaixo de 50).
   - O segundo ramo (`Weight4`, `Weight5`, `Weight6`, `Weight7`) trata o caso quando o RSI atual está na metade superior.
4. Transformar a probabilidade resultante em um sinal de operação entre `-1` e `+1`.
5. Se não há posição aberta e o sinal é negativo, comprar `TradeVolume` lotes. Se o sinal é não-negativo, vender `TradeVolume` lotes em vez disso.
6. Opcionalmente armar níveis simétricos de stop-loss e take-profit expressos em pips. A estratégia converte automaticamente a distância em pips para um deslocamento de preço absoluto, incluindo o ajuste de dígito extra usado pelo MetaTrader para símbolos forex de 3 e 5 dígitos.
7. Registrar cada decisão com as entradas RSI, probabilidade e sinal resultante, espelhando o comportamento informativo do expert de origem.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Período de 1 hora | Série de candles principal usada para atualizações de indicadores e geração de sinais. |
| `TradeVolume` | `decimal` | `1` | Tamanho de lote enviado com cada ordem de mercado. |
| `RsiPeriod` | `int` | `9` | Comprimento do indicador RSI. Também define a distância entre as amostras RSI históricas. |
| `AppliedPrice` | `AppliedPriceType` | `Open` | Componente de preço encaminhado ao RSI (Open, Close, High, Low, Median, Typical, Weighted). |
| `StopLossTakeProfitPips` | `decimal` | `100` | Distância em pips para stop-loss e take-profit. Definir como zero para desabilitar ordens de proteção. |
| `Weight0` … `Weight7` | `decimal` | `6, 96, 90, 35, 64, 83, 66, 50` | Pesos de probabilidade aplicados aos oito ramos da rede. Cada valor representa uma porcentagem entre 0 e 100. |

## Diferenças em relação ao expert original do MetaTrader
- As notificações por e-mail foram removidas. Os registros do StockSharp fornecem o mesmo insight sem depender de um servidor SMTP.
- O dimensionamento de posição está fixo em um único `TradeVolume`. Fechamentos parciais ou escalonamento incremental são omitidos intencionalmente para corresponder ao design de uma posição do código fonte.
- Os dados do indicador são entregues através da assinatura de candles de alto nível do StockSharp, eliminando chamadas manuais a `CopyBuffer` e aritmética de ponteiros.
- A conversão de pips usa o `PriceStep` do instrumento e compensa automaticamente os símbolos forex de 3/5 dígitos em vez de depender de tamanhos de tick codificados.

## Dicas de uso
- Alinhar `TradeVolume` com o passo mínimo de lote do instrumento antes de lançar a estratégia; o construtor também reflete o valor em `Strategy.Volume`.
- Ajustar os oito pesos durante a otimização para adaptar a rede de probabilidade a diferentes mercados. Todos os pesos são expostos como parâmetros de otimização.
- Diminuir `StopLossTakeProfitPips` ou defini-lo como zero ao operar em símbolos com spreads amplos ou ao usar saídas discricionais.
- Adicionar a estratégia a um gráfico para visualizar candles, RSI e operações executadas para facilitar a validação da saída da rede neural.

## Indicadores
- Um `RelativeStrengthIndex` calculado a partir do preço aplicado escolhido.
