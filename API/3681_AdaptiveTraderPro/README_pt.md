# Estratégia AdaptiveTrader Pro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
AdaptiveTrader Pro é uma estratégia de acompanhamento de tendências de vários períodos convertida do consultor especialista MetaTrader 5 *AdaptiveTrader_Pro_Final_EA.mq5*. Combina RSI, ATR e médias móveis para negociar na direção da tendência dominante enquanto aplica controles de gestão de dinheiro.

A estratégia funciona em um período primário configurável (padrão 5 minutos) e confirma a direção da tendência usando uma média móvel de período mais alto (padrão 1 hora). As entradas dependem de sinais de sobrevenda/sobrecompra RSI que concordam com ambas as médias móveis.

## Regras de negociação
- **Entrada Longa**: Quando RSI cai abaixo de 30 e o fechamento da vela está acima do período principal SMA e do período superior SMA.
- **Entrada curta**: Quando RSI sobe acima de 70 e o fechamento da vela está abaixo de ambos os SMAs.
- **Posição Única**: Apenas uma posição direcional é mantida por vez. As posições opostas são fechadas antes da reversão.

## Gestão de Risco e Comércio
- **Dimensionamento da posição**: o tamanho da posição é calculado a partir do patrimônio do portfólio, porcentagem de risco e distância de stop baseada em ATR.
- **Tratamento de Stop**: um trailing stop baseado em ATR segue o preço e é reduzido até o ponto de equilíbrio depois que a negociação se move a favor por um múltiplo ATR configurável.
- **Lucro Parcial**: Uma fração configurável da posição é fechada em um primeiro alvo (ATR múltiplo). O volume restante é gerenciado pelo trailing stop.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `MaxRiskPercent` | Porcentagem de risco aplicada à conta por negociação. | `0.2` |
| `RsiPeriod` | Duração de RSI no período principal. | `14` |
| `AtrPeriod` | Duração de ATR no período principal. | `14` |
| `AtrMultiplier` | Multiplicador ATR para a distância de parada inicial. | `1.5` |
| `TrailingStopMultiplier` | Multiplicador ATR usado ao rastrear o stop. | `1.0` |
| `TrailingTakeProfitMultiplier` | Multiplicador de ATR para a meta de lucro parcial. | `2.0` |
| `TrendPeriod` | Duração de SMA no período principal. | `20` |
| `HigherTrendPeriod` | Duração de SMA no período de tempo superior. | `50` |
| `BreakEvenMultiplier` | Multiplicador ATR que aciona a movimentação do stop para o ponto de equilíbrio. | `1.5` |
| `PartialCloseFraction` | Fração da posição inicial fechada no primeiro alvo. | `0.5` |
| `MaxSpreadPoints` | Spread máximo permitido nas etapas de preço antes de abrir negociações. | `20` |
| `CandleType` | Tipo de vela principal (prazo) usado para análise. | `5 minute candles` |
| `HigherCandleType` | Tipo de vela de prazo superior usado para confirmação. | `1 hour candles` |

## Notas
- A estratégia usa StockSharp API de alto nível com assinaturas de velas e vinculação de indicadores.
- Os spreads são monitorados através das melhores cotações bid/ask; a negociação fica suspensa até que o spread esteja dentro do limite configurado.
- A implementação do Python é omitida intencionalmente por instruções.
