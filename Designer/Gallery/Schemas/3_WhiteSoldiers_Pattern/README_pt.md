# Exemplo de Detecção do Padrão Three White Soldiers no StockSharp Strategy Designer
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Este exemplo demonstra a implementação de uma estratégia de negociação no StockSharp Strategy Designer que utiliza o padrão de candles "Three White Soldiers". Este padrão é frequentemente interpretado como um sinal de reversão de alta e pode ser decisivo para traders que buscam capitalizar mudanças de momento. A configuração descrita no esquema JSON envolve a detecção deste padrão e o início de negociações com base em sua ocorrência.

![schema](schema.png)

## Descrição do esquema

O esquema descreve um fluxo de trabalho complexo projetado para detectar o [padrão](https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/pattern.html) "Three White Soldiers" e executar negociações de acordo. Aqui estão os componentes principais e seus papéis:

1. **Nó Security**: Especifica o [ativo](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) ao qual a estratégia é aplicada. Atua como a principal fonte de entrada de dados, fornecendo os dados de mercado necessários para a análise subsequente.

2. **Nó TimeFrameCandle**: Gera [dados de candles](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) para o ativo especificado. Este nó é crucial, pois processa os dados de mercado recebidos em um formato utilizável (candles) que o algoritmo de detecção de padrões pode analisar.

3. **Nó de Detecção de Padrão**: Configurado especificamente para detectar o [padrão](https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/pattern.html) "Three White Soldiers" via [indicador](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html). Este nó analisa os dados de candles e aciona uma ação quando o padrão é identificado.

4. **Nó Chart Panel**: Visualiza os dados de negociação, incluindo padrões de candles e possivelmente as negociações executadas pela estratégia. Este [componente](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html) ajuda a monitorar o desempenho da estratégia e a entender como o padrão influencia as decisões de negociação.

5. **Nós de Negociação (Compra, Venda)**: Estes [nós](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) estão configurados para executar negociações quando o padrão é detectado. As ações podem variar com base em condições adicionais definidas dentro da estratégia, como condições de mercado ou outros indicadores técnicos.

## Fluxo de trabalho

- O **Nó Security** alimenta dados de mercado no **Nó TimeFrameCandle**, onde os dados são transformados em candles.
- Esses candles são então passados para o **Nó de Detecção de Padrão**, configurado para identificar o padrão "Three White Soldiers".
- Ao detectar o padrão, o nó pode acionar um ou mais **Nós de Negociação** para executar ordens de compra ou venda, dependendo do design da estratégia.
- O **Nó Chart Panel** fornece uma visualização em tempo real dos candles e das negociações executadas, ajudando a avaliar a eficácia da estratégia e a fazer ajustes quando necessário.

## Aplicação prática

Esta configuração é particularmente útil para traders que se especializam em estratégias baseadas em momento, onde o reconhecimento precoce de padrões pode levar a ganhos significativos. O padrão "Three White Soldiers" é um forte indicador de reversão de alta, tornando esta estratégia adequada para:
- Swing trading em mercados onde as mudanças de momento são bruscas e claras.
- Day trading em mercados altamente voláteis, onde o reconhecimento precoce de reversões de tendência pode levar a negociações rentáveis.

## Conclusão

Este exemplo do StockSharp Strategy Designer ilustra um uso sofisticado da detecção de padrões de candles no contexto do trading algorítmico. Ao automatizar a detecção de padrões como o "Three White Soldiers", os traders podem se posicionar de forma mais eficaz no mercado, aproveitando o poder preditivo dos padrões históricos de preços. A visualização detalhada e o processamento de dados em tempo real também auxiliam no refinamento da estratégia com base nas condições de mercado observadas e nos resultados.
