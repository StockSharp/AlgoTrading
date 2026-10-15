# Estratégia Adaptativa Perceptron
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral

Esta estratégia é um port para StockSharp do assessor especialista do MetaTrader 5 *Perceptron.mq5*.  
Cinco sinais discretos de indicadores são combinados por meio de um perceptron de duas camadas. Cada trade registra o estado do indicador e, uma vez que a posição é fechada, os pesos sinápticos são reforçados ou penalizados dependendo do lucro obtido. O comportamento imita o loop de autoaprendizagem do EA original aproveitando a API de candles de alto nível do StockSharp.

## Camada de indicadores

| Código | Descrição | Lógica de sinal |
| --- | --- | --- |
| `IND1` | Cruzamento de médias móveis simples rápida/lenta | +1 quando a MA rápida cruza acima da MA lenta na barra anterior, −1 em um cruzamento descendente, caso contrário 0. |
| `IND2` | Índice de Força Relativa (RSI) | +1 quando o RSI sai da zona de sobrevenda (cruza acima de 30), −1 quando o RSI sai da zona de sobrecompra (cruza abaixo de 70). |
| `IND3` | Índice do Canal de Commodities (CCI) | +1 em um cruzamento acima de −100, −1 em um cruzamento abaixo de +100. |
| `IND4` | Inclinação da média móvel simples curta | +1 se a MA curta aumentou entre as duas barras anteriores, −1 se diminuiu. |
| `IND5` | Cor do momentum do Awesome Oscillator | +1 quando o histograma aumenta em comparação com o valor anterior (cor altista), −1 quando diminui. |

Todos os indicadores são avaliados em candles fechados. Buffers históricos são mantidos internamente para replicar o windowing `CopyBuffer` usado no script MQL5.

## Arquitetura do perceptron

- Cinco neurônios ocultos (`NN1`…`NN5`) combinam quatro indicadores cada um, imitando a fiação no EA.
- Cada neurônio tem seu próprio dicionário de pesos sinápticos mais um peso de viés (`NNS1`…`NNS5`).
- A ativação final `brainReturn` é a soma ponderada das saídas dos neurônios.  
  - `brainReturn > 0` → solicitar entrada comprada (se o trade anterior também não foi comprado).  
  - `brainReturn < 0` → solicitar entrada vendida (se o trade anterior também não foi vendido).
- Posições são abertas apenas com ordens de mercado quando nenhuma posição está ativa.

## Gestão de posição

- Preço de entrada, direção e estados de indicador/neurônio são capturados em cada execução.
- Os deslocamentos de take-profit e stop-loss são aplicados em unidades de preço absoluto (ex.: 0.0004 para 4 pontos em um par Forex com 5 decimais).  
  Quando um novo candle abre após a entrada:
  - Para comprados, a máxima é comparada com o preço de take-profit primeiro, depois a mínima com o stop-loss.  
  - Para vendidos, a mínima é comparada com o preço de take-profit primeiro, depois a máxima com o stop-loss.  
  - Se ambos os níveis forem excedidos dentro do mesmo candle, o take-profit tem prioridade, correspondendo ao comportamento otimista do EA original.
- Uma vez detectada uma saída, a estratégia fecha a posição com uma ordem de mercado e calcula o lucro realizado usando o nível TP/SL correspondente.

## Atualização adaptativa de pesos

Quando um trade fecha, os estados de indicador e neurônio capturados são reproduzidos:

1. `directionSign` (−1 para comprados, +1 para vendidos) e `outcomeSign` (sinal do PnL realizado) são determinados.
2. Pesos de viés são ajustados dentro de `[SinMin, SinMax]`:
   - Se `sign(neuronOutput) * directionSign` for positivo, o viés segue o resultado do trade (aumenta no lucro, diminui na perda).
   - Caso contrário, o viés se move oposto ao resultado.
3. Pesos sinápticos se comportam de forma semelhante, mas permanecem sem limites: sinais alinhados com a direção da posição recebem reforço nos lucros e penalidades nas perdas, enquanto sinais opostos fazem o inverso.
4. Sinais armazenados são apagados para evitar reutilização acidental.

Isso generaliza as mais de 1.500 linhas de gerenciamento condicional de sinapses do EA em uma rotina de reforço compacta.

## Parâmetros

| Parâmetro | Padrão | Descrição |
| --- | --- | --- |
| `CandleType` | Período de 1 minuto | Assinatura de candles usada pela estratégia. |
| `FastMaLength` | 5 | Período da SMA rápida no sinal de cruzamento. |
| `SlowMaLength` | 9 | Período da SMA lenta. |
| `RsiLength` | 14 | Período de cálculo do RSI. |
| `CciLength` | 14 | Período de cálculo do CCI. |
| `SlopeMaLength` | 5 | Período da MA usada para detecção de inclinação. |
| `AoShortLength` | 5 | Período curto do Awesome Oscillator. |
| `AoLongLength` | 34 | Período longo do Awesome Oscillator. |
| `StopLossOffset` | 0.001 | Distância de stop-loss em unidades de preço absoluto (0 desativa o stop). |
| `TakeProfitOffset` | 0.0004 | Distância de take-profit em unidades de preço absoluto (0 desativa o alvo). |
| `SinMax` | 5 | Limite superior para pesos de viés do neurônio. |
| `SinMin` | 0 | Limite inferior para pesos de viés do neurônio. |
| `SinPlusStep` | 0.03 | Incremento de reforço positivo. |
| `SinMinusStep` | 0.03 | Decremento de reforço negativo. |

Todos os parâmetros numéricos são expostos como `StrategyParam<T>` e podem ser otimizados no StockSharp Designer.

## Notas de implementação

- Usa a API de assinatura de candles de alto nível com vinculação de múltiplos indicadores.
- O gerenciamento manual de trades é empregado para que os preços realizados sejam conhecidos ao atualizar sinapses.
- Históricos de indicadores são armazenados com campos anuláveis para garantir que os sinais só disparem após formação completa.
- O buffer de cor do Awesome Oscillator no EA é aproximado comparando os valores atual e anterior do histograma.
- A saída do gráfico desenha a série de candles mais as médias móveis rápida e lenta. Marcadores de trade mostram o comportamento adaptativo em tempo real.

## Limitações e suposições

- Stops e alvos são avaliados uma vez por candle concluído; a ordem intrabar de eventos é desconhecida, por isso a prioridade é dada ao alvo de lucro quando ambos os limiares são atingidos.
- Pesos de indicadores são ilimitados como no EA original e podem crescer significativamente durante ciclos de reforço prolongados.
- O `LastTradeType` do EA original nunca foi redefinido; neste port é limpo após cada saída para que trades consecutivos na mesma direção permaneçam possíveis.
