# Descrição da Estratégia PseudoIndex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral da Estratégia

A estratégia "PseudoIndex" foi desenvolvida para criar um índice sintético a partir das razões de preços de duas criptomoedas principais, especificamente Ethereum e Bitcoin, negociadas na exchange Binance. Esta estratégia monitora o desempenho relativo dessas criptomoedas calculando um índice em tempo real com base em seus movimentos de preços.

![schema](schema.png)

## Detalhes da Estratégia

### Componentes

- **Fontes de Dados**: utiliza dados de [preço em tempo real](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) de ETHUSDT e BTCUSDT da Binance.
- **Cálculo de Preço**:
  - Rastreia os [preços de fechamento](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) tanto de ETHUSDT quanto de BTCUSDT.
  - Calcula a razão desses preços para formar um índice sintético, representando o desempenho relativo do Ethereum em relação ao Bitcoin.

### Cálculo do Índice

- **Formação de Velas**: usa um [intervalo de tempo de 5 minutos](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) para ETH e BTC para capturar movimentos de preços de curto prazo.
- **Cálculo da Razão**: o índice é calculado como o preço do ETH [dividido](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/formula.html) pelo preço do BTC, fornecendo uma medida de como o valor do Ethereum evolui em relação ao Bitcoin.

### Visualização

- **Exibição no Gráfico**: o índice resultante é plotado em um [gráfico](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html) para análise visual, ajudando a identificar tendências e possíveis sinais de trading com base no movimento do índice.

## Detalhes de Implementação

- **Plataforma**: implementada dentro da plataforma StockSharp utilizando seus recursos avançados para obtenção e processamento de dados em tempo real.
- **Indicadores Técnicos**: a estratégia se baseia em informações básicas de preços sem o uso de indicadores técnicos adicionais, focando na razão de preços para a tomada de decisões.

## Conclusão

A estratégia "PseudoIndex" oferece uma abordagem inovadora ao trading, comparando o desempenho de duas criptomoedas principais, permitindo que os traders avaliem o sentimento do mercado e tomem decisões informadas com base na força relativa do Ethereum e do Bitcoin. Isso pode ser particularmente útil para traders que buscam fazer hedge ou diversificar suas posições em criptomoedas com base nessas análises.
