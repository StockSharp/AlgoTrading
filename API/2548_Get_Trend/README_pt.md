# Estratégia de Seguidor de Tendência (Get Trend)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é um port em StockSharp do consultor especialista do MetaTrader **"Get trend"**, originalmente projetado para operar em M15 com um filtro de confirmação em H1. O algoritmo combina médias móveis suavizadas e um oscilador estocástico para temporizar entradas de reversão à média alinhadas com a tendência de um período superior.

## Lógica de trading

- **Período principal:** Velas de 15 minutos são usadas para geração de sinais e execução de ordens.
- **Período de confirmação:** Velas horárias fornecem a média móvel suavizada e o preço de fechamento do período superior para validar a tendência predominante.
- **Filtro de tendência:** Tanto o fechamento do M15 quanto o do H1 devem estar no mesmo lado de suas respectivas médias móveis suavizadas. Adicionalmente, o fechamento do M15 deve permanecer dentro de uma distância configurável de sua média móvel para garantir uma entrada em recuo.
- **Gatilho de momentum:** Operações longas requerem que a linha %K do estocástico cruze acima de %D na região de sobrevenda (abaixo de 20). Operações curtas requerem o cruzamento inverso na região de sobrecompra (acima de 80).
- **Gestão de ordens:** As posições são protegidas com níveis fixos de stop-loss e take-profit definidos em pontos de preço. Um trailing stop opcional aperta a saída assim que o preço avança o suficiente a favor da operação.

## Critérios de entrada

### Configuração comprada
1. O fechamento de M15 está abaixo da média móvel suavizada de M15.
2. O fechamento de H1 está abaixo da média móvel suavizada de H1.
3. A distância entre o fechamento de M15 e a média de M15 não excede o **Price Threshold** (expresso em pontos/ticks).
4. O %K e %D do estocástico estão ambos abaixo de 20.
5. O valor anterior de %K estava abaixo de %D, e o %K atual cruzou acima de %D.
6. Não há posição comprada existente (uma posição vendida será fechada e revertida).

### Configuração vendida
1. O fechamento de M15 está acima da média móvel suavizada de M15.
2. O fechamento de H1 está acima da média móvel suavizada de H1.
3. A distância entre o fechamento de M15 e a média de M15 não excede o **Price Threshold**.
4. O %K e %D do estocástico estão ambos acima de 80.
5. O valor anterior de %K estava acima de %D, e o %K atual cruzou abaixo de %D.
6. Não há posição vendida existente (uma posição comprada será fechada e revertida).

## Regras de saída

- **Stop-loss:** Definido em pontos de preço absolutos a partir do preço de entrada.
- **Take-profit:** Definido em pontos de preço absolutos a partir do preço de entrada.
- **Trailing stop:** Quando habilitado, assim que o preço avança além da distância de trailing, o stop é puxado para trás para travar os lucros respeitando o offset de trailing configurado.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `M15CandleType` | Tipo de vela usado para geração de sinais. | Período de 15 minutos |
| `H1CandleType` | Tipo de vela usado para confirmação. | Período de 1 hora |
| `MaM15Length` | Comprimento da MA suavizada em velas M15. | 99 |
| `MaH1Length` | Comprimento da MA suavizada em velas H1. | 184 |
| `StochasticLength` | Período %K do oscilador estocástico. | 27 |
| `StochasticSignalLength` | Período de suavização %D. | 3 |
| `ThresholdPoints` | Distância máxima (em pontos) entre o preço e a MA do M15 para permitir entradas. | 10 |
| `TakeProfitPoints` | Distância de take-profit (em pontos). | 540 |
| `StopLossPoints` | Distância de stop-loss (em pontos). | 90 |
| `TrailingStopPoints` | Distância de trailing stop (em pontos). | 20 |
| `TradeVolume` | Volume base da ordem ao abrir novas operações. | 0.1 |

Todos os parâmetros baseados em pontos são multiplicados pelo `PriceStep` do instrumento para convertê-los em incrementos de preço absolutos.

## Notas de implementação

- A estratégia usa a API de alto nível do StockSharp com assinaturas de velas e vinculação de indicadores (`BindEx`) para evitar o gerenciamento manual de buffers.
- A lógica do trailing stop replica a versão do MetaTrader: ativa-se assim que o lucro não realizado excede a distância de trailing e continua ajustando o stop em direção ao preço.
- As ordens ativas são canceladas antes de reverter posições para evitar ordens conflitantes no livro.
- As áreas do gráfico exibem velas M15 com a média móvel suavizada e um painel estocástico dedicado para diagnósticos visuais.

## Dicas de uso

- Configure os tipos de velas para corresponder ao provedor de dados (p. ex., velas baseadas em volume podem ser substituídas se expuserem o mesmo conceito de DataType).
- Ajuste o limiar e os parâmetros de stop ao operar com ativos de diferente volatilidade ou tamanhos de tick.
- Para melhores resultados, aplique a estratégia a instrumentos com tendência onde recuos em direção à média móvel são comuns.
