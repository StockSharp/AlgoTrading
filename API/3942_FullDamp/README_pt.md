# Estratégia de umidade total
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia Full Damp é um sistema de reversão de tendência construído em torno de um conjunto triplo de Bollinger bandas combinadas com um filtro de confirmação do Índice de Força Relativa (RSI). A estratégia espera por picos de preços além da banda mais ampla Bollinger para detectar uma possível exaustão. Uma leitura recente de sobrevenda ou sobrecompra RSI valida o sinal antes que a negociação seja acionada quando o preço retorna dentro da banda de largura média. Uma vez posicionadas, as saídas são gerenciadas com realização de lucro parcial, ajustes de stop dinâmicos e regras de rastreamento baseadas em Bollinger.

## Lógica de negociação

1. **Detecção de sinal**
   * As configurações longas aparecem quando a mínima da vela fecha na ou abaixo da banda inferior de um conjunto Bollinger com largura 3. As configurações curtas ocorrem quando a máxima da vela atinge a banda superior do mesmo conjunto.
   * O RSI deve ter atingido o limite de sobrevenda (comprada) ou sobrecompra (curta) nas últimas *Barras Lookback*. Esta condição é monitorada continuamente, portanto, um novo extremo RSI atualiza a contagem regressiva.
2. **Acionador de entrada**
   * Uma posição longa é aberta quando o preço fecha acima da banda inferior do conjunto médio Bollinger (largura 2), desde que nenhuma posição já esteja aberta.
   * Uma posição curta é aberta depois que o preço fecha abaixo da banda superior do conjunto médio Bollinger.
   * Os níveis iniciais de stop-loss são ancorados na mínima mais baixa (para posições compradas) ou na máxima mais alta (para posições vendidas) vista desde a vela de sinal, expandida pelo deslocamento de ponto configurável.
3. **Gerenciamento de posição**
   * Quando o mercado atinge um lucro igual ao risco inicial, metade da posição é fechada e o stop-loss é movido para o ponto de equilíbrio.
   * O volume restante será encerrado se a máxima da vela (para posições compradas) ou mínima (para posições vendidas) cruzar a banda média Bollinger na direção oposta.
   * Se o preço retornar ao nível de stop antes que uma meta de lucro seja alcançada, toda a posição será fechada.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `CandleType` | Fonte de dados Candle usada para análise e execução. | Velas horárias |
| `BollingerPeriod1` | Período das bandas estreitas Bollinger (largura = 1). | 20 |
| `BollingerPeriod2` | Período das bandas médias Bollinger (largura = 2). | 20 |
| `BollingerPeriod3` | Período das bandas largas Bollinger (largura = 3). | 20 |
| `RsiPeriod` | RSI período usado para confirmação do sinal. | 14 |
| `LookbackBars` | Número de velas concluídas dentro das quais o RSI deve atingir os níveis extremos. | 6 |
| `StopOffsetPoints` | Buffer adicional (em pontos de preço) adicionado ao nível inicial de stop loss. | 10 |
| `Volume` | Volume de pedidos herdado da estratégia base. | 1 |

## Notas

* Os limites RSI são fixados em 30 para sinais longos e 70 para sinais curtos para imitar a lógica MQL original.
* A estratégia usa o StockSharp API de alto nível: os indicadores estão vinculados à assinatura da vela, o gerenciamento comercial usa ordens de mercado e a lógica de proteção é tratada internamente sem pesquisa manual de valor do indicador.
* Saídas parciais e ajustes de stop são executados no fechamento da vela para manter o comportamento alinhado com a implementação original.
