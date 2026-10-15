# Estratégia Cruzada do Coelho 3MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
O **ThreeMaBunnyCrossStrategy** é uma conversão do MetaTrader 4 consultor especialista "3MA Bunny Cross". Ele negocia reversões de tendência com base no cruzamento entre duas médias móveis lineares ponderadas (LWMAs) calculadas nos preços de fechamento do período selecionado. A versão StockSharp mantém a ideia original de reverter a posição imediatamente após um cruzamento e adiciona conveniências de alto nível API, como vinculação de indicadores e proteção de risco integrada.

## Descrição original MQL
O consultor especialista fonte usa dois LWMAs com períodos 5 e 20. Quando o LWMA rápido cruza o LWMA lento, o consultor fecha a posição oposta, se existir, e imediatamente abre uma nova negociação na direção do cruzamento. Apenas uma posição é permitida a cada momento. O script original também verifica um número mínimo de barras e margem livre antes da negociação.

## StockSharp Detalhes de implementação
- A estratégia assina velas definidas pelo parâmetro `CandleType` (período de 15 minutos por padrão) e as vincula a dois indicadores `LinearWeightedMovingAverage`.
- Os valores dos indicadores são fornecidos diretamente ao método de processamento por meio de `Bind`, eliminando a necessidade de manipulação manual do buffer.
- Os valores rápidos e lentos anteriores são armazenados em cache para detectar cruzamentos usando a mesma lógica da versão MQL (`fast` cruzamento acima ou abaixo de `slow`).
- Quando ocorre um cruzamento de alta e a posição atual é plana ou curta, a estratégia envia uma ordem de compra de mercado dimensionada para fechar qualquer exposição curta e abrir uma nova posição longa (`Volume + |Position|`). O cruzamento de baixa se comporta simetricamente para vendas.
- `StartProtection()` é chamado uma vez no início para ativar rotinas integradas de proteção de posição.
- A visualização do gráfico desenha as velas de origem junto com as duas médias móveis e as negociações da própria estratégia.

## Parâmetros
- **CandleType** – tipo de dados que descreve a série de velas a ser assinada (o padrão é um período de 15 minutos).
- **FastPeriod** – período do LWMA rápido. Padrão: 5. Otimizável.
- **SlowPeriod** – período do LWMA lento. Padrão: 20. Otimizável.

## Indicadores
- `LinearWeightedMovingAverage` (rápido, período 5 por padrão).
- `LinearWeightedMovingAverage` (lento, período 20 por padrão).

## Regras de negociação
1. Aguarde o término da vela e verifique se a estratégia está formada, online e com permissão para negociação.
2. Detecte um cruzamento de alta quando o LWMA rápido estiver abaixo ou igual ao LWMA lento na vela anterior e estiver acima ou igual a ele na vela atual. Neste caso, feche qualquer posição curta existente e abra uma longa.
3. Detecte um cruzamento de baixa quando o LWMA rápido estiver acima ou igual ao LWMA lento na vela anterior e estiver abaixo ou igual a ele na vela atual. Neste caso, feche qualquer posição longa existente e abra uma posição curta.
4. Cada novo tamanho de pedido é calculado como `Volume + |Position|` para reverter totalmente qualquer exposição pendente, garantindo que exista apenas uma posição direcional por vez.
