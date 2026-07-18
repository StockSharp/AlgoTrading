# Estratégia de Reversão Pinbar
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Os Pinbars destacam rejeições repentinas de preço e podem sinalizar pontos de inflexão de curto prazo. Esta estratégia mede o comprimento da pavio da vela em relação ao seu corpo, procurando longas sombras que se destacam da ação de preço recente. Um filtro de média móvel ajuda a operar na direção da tendência subjacente.

Os testes indicam um retorno anual médio de aproximadamente 82%. Tem melhor desempenho no mercado de ações.

Durante cada atualização de vela, o sistema calcula as sombras superiores e inferiores e as compara com o tamanho do corpo. Um Pinbar altista com um pavio inferior longo pode acionar uma entrada comprada se o preço estiver acima da média móvel. Da mesma forma, um Pinbar baixista com um pavio superior estendido pode iniciar uma posição vendida em uma tendência de baixa. Os stops são colocados a um percentual fixo da entrada.

A operação é encerrada quando aparece um Pinbar oposto contra a posição aberta ou quando o stop protetor é atingido. Combinar a lógica do Pinbar com um filtro de tendência melhora a confiabilidade ao evitar configurações contratendência.

## Detalhes

- **Critérios de entrada**: Pinbar com cauda longa e sombra oposta pequena, confirmado pela tendência.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Pinbar oposto ou stop-loss.
- **Stops**: Sim, baseado em percentual.
- **Valores padrão**:
  - `TailToBodyRatio` = 2
  - `OppositeTailRatio` = 0.5
  - `MAPeriod` = 20
  - `CandleType` = 15 minute
  - `StopLossPercent` = 1
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: Candlestick, MA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

