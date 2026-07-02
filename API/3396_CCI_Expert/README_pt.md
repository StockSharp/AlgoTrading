# CCI Estratégia especializada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é uma StockSharp conversão do robô MetaTrader "CCI-Expert" original. Ele usa o indicador Commodity Channel Index (CCI) em um único período de tempo e mantém a lógica estritamente sequencial: a estratégia espera por três velas concluídas antes de decidir abrir ou fechar uma posição.

## Lógica de negociação

1. Assine a série de velas configurada e calcule um CCI com o período escolhido.
2. Avalie os três últimos valores CCI concluídos:
   - **Configuração longa**: os valores CCI atuais e anteriores estão acima de `+1`, enquanto o segundo valor anterior estava abaixo de `+1`.
   - **Configuração curta**: os valores CCI atuais e anteriores estão abaixo de `+1`, enquanto o segundo valor anterior estava acima de `+1`.
3. Abra apenas uma posição de mercado por vez quando nenhuma posição estiver ativa e o filtro de spread permitir a negociação.
4. Feche uma posição existente somente se o sinal oposto aparecer **e** a negociação já for lucrativa (o preço de fechamento é melhor que o preço de entrada).

## Gestão de risco

- A estratégia pode usar um lote fixo ou calcular o volume a partir da porcentagem de risco e da distância de stop-loss configurada.
- `StartProtection` coloca automaticamente faixas de stop-loss e take-profit em faixas de preço.
- Um filtro de spread opcional bloqueia a negociação até que a diferença atual de compra/venda esteja abaixo do limite de `MaxSpreadPoints`.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `FixedVolume` | Tamanho fixo do pedido. Set to zero to activate risk-based sizing. | 0,1 |
| `RiskPercent` | Porcentagem do valor atual do portfólio usado para dimensionar pedidos quando `FixedVolume` é zero. | 0 |
| `TakeProfitPoints` | Distância de lucro medida em faixas de preço. | 150 |
| `StopLossPoints` | Distância de stop-loss medida em pontos de preço. | 600 |
| `MaxSpreadPoints` | Spread máximo permitido (em faixas de preço). Zero desativa o filtro. | 30 |
| `CciPeriod` | Período de lookback do indicador CCI. | 14 |
| `CandleType` | Prazo das velas processadas pela estratégia. | Velas de 15 minutos |

## Notas

- O limite CCI permanece constante em `+1` e `-1`, assim como a fonte MQL, portanto, as negociações são acionadas somente após um padrão claro de três etapas.
- Como o dimensionamento do volume baseado em risco depende de metadados do instrumento (`PriceStep`, `StepPrice`, `VolumeStep`, etc.), certifique-se de que esses valores estejam disponíveis na placa conectada.
- A estratégia desenha velas, a linha do indicador CCI e executa negociações no gráfico para facilitar a depuração visual.
