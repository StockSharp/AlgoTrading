# Estratégia Expotest
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia Expotest é uma conversão direta para StockSharp do assessor especialista original `Expotest.mq5`. Ela opera um único instrumento usando o indicador Parabolic SAR e uma regra simples de gerenciamento monetário inspirada no martingale. A estratégia abre apenas uma posição de cada vez e depende de níveis predefinidos de stop-loss e take-profit para as saídas.

## Lógica de trading
- **Indicador**: Parabolic SAR calculado na série de velas selecionada. Tanto o fator de aceleração (`SarStep`) quanto a aceleração máxima (`SarMaximum`) são configuráveis.
- **Condições de entrada**: Quando nenhuma posição está aberta, a estratégia verifica a última vela fechada.
  - Se o valor do Parabolic SAR estiver abaixo ou igual ao preço de fechamento, uma posição comprada é iniciada.
  - Se o valor do Parabolic SAR estiver acima ou igual ao preço de fechamento, uma posição vendida é iniciada.
- **Condições de saída**: Os níveis de stop-loss e take-profit são colocados a uma distância fixa do preço de entrada, medida em passos de preço. Durante cada nova vela, a estratégia monitora se o intervalo da vela toca qualquer nível e fecha a posição de acordo. O tipo de saída (lucro ou perda) é lembrado para futuras decisões de dimensionamento de posição.

## Dimensionamento de posição
- **Volume base**: Definido pelo parâmetro `FixedVolume` quando for maior que zero. Caso contrário, a estratégia estima o tamanho a partir dos valores `RiskPercent` e `StopLossPoints` usando o capital atual do portfólio. Se nenhum método retornar um tamanho válido, o `Strategy.Volume` padrão é usado.
- **Passo martingale**: Após uma operação perdedora, o próximo tamanho de posição é dobrado em comparação com o volume da posição perdedora. Uma saída lucrativa redefine o multiplicador e a próxima ordem usa o volume base novamente.

## Parâmetros configuráveis
- `CandleType` – Tipo de dados para agregação de velas (período ou outro formato de vela).
- `SarStep` – Fator de aceleração inicial para o Parabolic SAR.
- `SarMaximum` – Fator de aceleração máximo para o Parabolic SAR.
- `StopLossPoints` – Distância do stop-loss desde a entrada expressa em passos de preço.
- `TakeProfitPoints` – Distância do take-profit desde a entrada expressa em passos de preço.
- `RiskPercent` – Porcentagem do capital do portfólio a arriscar por operação quando o dimensionamento dinâmico está habilitado.
- `FixedVolume` – Volume de ordem explícito. Definir como `0` para habilitar o dimensionamento baseado em risco.

## Notas adicionais
- A estratégia processa apenas velas terminadas para se manter próxima da implementação MQL original baseada em ticks enquanto permanece compatível com as subscrições do StockSharp.
- Os níveis de proteção são rastreados internamente em vez de ordens stop/limit separadas, o que mantém a lógica transparente e fácil de backtestear.
- A implementação Python é intencionalmente omitida conforme solicitado.
