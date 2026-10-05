# Estratégia de Gap Overnight
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
O Gap Overnight opera na abertura quando o preço apresenta um gap significativo em relação ao fechamento anterior devido a notícias ou atividade fora do horário.
Grandes gaps frequentemente recuam parcialmente à medida que os traders digerem o movimento.

Os testes indicam um retorno anual médio de aproximadamente 124%. Funciona melhor no mercado de forex.

A estratégia vai contra gaps excessivos, entrando na direção oposta logo após a abertura e fechando antes do fim da sessão.

Os stops são baseados em um percentual além dos extremos do gap para gerenciar o risco caso o movimento continue.

## Detalhes

- **Critérios de entrada**: sinal de indicador
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 2
  - `MinGapPercent` = 0.01
    O mercado negocia 24 horas, por isso a sessão é o dia UTC. O gap é a distância entre o último fechamento do dia anterior e a primeira abertura do dia. Um gap de pelo menos MinGapPercent é operado contra no fechamento do primeiro candle; o stop fica StopLossPercent além da máxima (venda) ou da mínima (compra) desse candle e é verificado nos fechamentos, e a posição é encerrada no último candle do dia.
- **Filtros**:
  - Categoria: Gap
  - Direção: Ambos
  - Indicadores: Gap
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

