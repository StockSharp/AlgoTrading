# MACD Exemplo de estratégia de grade de hedge
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma versão StockSharp do consultor especialista MetaTrader "MACD Sample Hedging Grid". Ele combina um cruzamento MACD de curto prazo, um filtro de inclinação local EMA e confirmações de período de tempo mais alto. Quando as condições se alinham, a estratégia constrói uma grade de posições na direção detectada, dimensionando o tamanho da negociação por um expoente configurável.

## Lógica de Mercado
- **Período base:** configurável (velas padrão de 5 minutos).
- **Filtro de tendência:** um EMA (padrão 26 períodos) deve inclinar-se para cima para negociações longas ou para baixo para negociações curtas.
- **MACD gatilho:** a linha rápida MACD deve cruzar a linha de sinal no período base enquanto excede um valor absoluto mínimo (expresso em etapas de preço).
- **Confirmação de impulso:** a distância absoluta entre o impulso e o nível neutro 100 em um período de tempo mais alto deve exceder limites separados para posições compradas e vendidas. As últimas três velas de prazo superior são inspecionadas, replicando o comportamento original EA.
- **Confirmação de longo prazo:** um MACD calculado em um período de tempo longo (mensal por padrão) deve concordar com a direção da negociação (MACD acima do sinal para alta, abaixo para ambientes de baixa).

Assim que um sinal é acionado, a estratégia inicia uma nova grade naquela direção ou adiciona à grade existente, desde que o número máximo de entradas não tenha sido atingido.

## Gerenciamento de posição
- **Dimensionamento da grade:** cada entrada adicional multiplica o volume inicial por `LotExponent` (padrão 1,44). O tamanho da posição é redefinido quando a direção muda ou a posição é fechada.
- **Controles de risco:** distâncias opcionais de take-profit e stop-loss são traduzidas em StockSharp ordens de proteção em etapas de preço.
- **Mudança de direção:** sempre que chega um sinal oposto, a exposição atual é nivelada antes de abrir a grade na nova direção.

## Parâmetros
| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `CandleType` | Prazo principal usado para cálculos de MACD e EMA. | Período de 5 minutos |
| `MomentumCandleType` | Prazo mais longo alimentando a confirmação do momentum. | Período de 30 minutos |
| `TrendCandleType` | Período longo usado para o filtro de tendência MACD. | Prazo de 30 dias |
| `FastMaPeriod` | Comprimento EMA rápido dentro de MACD. | 12 |
| `SlowMaPeriod` | Comprimento EMA lento dentro de MACD. | 26 |
| `SignalPeriod` | Comprimento do sinal SMA para MACD. | 9 |
| `TrendMaPeriod` | Comprimento EMA para o filtro de tendência local. | 26 |
| `MomentumPeriod` | Duração do indicador de momentum (período de tempo maior). | 14 |
| `MacdOpenLevel` | Nível absoluto mínimo MACD (em etapas de preço) necessário para uma negociação. | 3 |
| `MomentumBuyThreshold` | Distância mínima do momento absoluto de 100 para posições compradas. | 0,3 |
| `MomentumSellThreshold` | Distância mínima do momento absoluto de 100 para shorts. | 0,3 |
| `MaxTrades` | Número máximo de entradas de grade por direção. | 10 |
| `LotExponent` | Multiplicador usado para cada entrada adicional na grade. | 1,44 |
| `StopLossSteps` | Distância de stop-loss medida em etapas de preço. | 20 |
| `TakeProfitSteps` | Distância de lucro medida em etapas de preço. | 50 |

## Notas
- O EA original também continha rastreamento baseado em dinheiro, movimentos de ponto de equilíbrio e paradas de patrimônio da conta. Esses recursos exigem dados de portfólio específicos da corretora e gerenciamento manual de pedidos; eles não são implementados nesta conversão StockSharp de alto nível.
- Assinaturas de velas, vinculações de indicadores e execução de negociações seguem o uso de API de alto nível recomendado por API.
- Certifique-se de que os instrumentos selecionados suportam os tipos de velas configurados e que os dados históricos estão disponíveis para todos os prazos referenciados.
