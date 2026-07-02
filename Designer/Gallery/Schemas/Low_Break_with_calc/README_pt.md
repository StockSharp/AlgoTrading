# Descrição da Estratégia de Rompimento de Mínimas com Cálculo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral da Estratégia

A estratégia "Rompimento de Mínimas com Cálculo" utiliza uma combinação de indicadores de preço máximo e mínimo para identificar possíveis pontos de rompimento no mercado. Esta estratégia tem como objetivo executar operações quando o preço rompe abaixo de uma mínima calculada durante um período específico, sugerindo uma possível tendência de baixa.

[![schema](schema.png)](schema_easter_egg.png)

## Detalhes da Estratégia

### Componentes

- **Formação de Velas**: utiliza um intervalo de tempo de uma hora para a geração de [velas](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html), capturando movimentos de mercado significativos.
- **Indicadores de Máximas e Mínimas**:
  - **Highest 25**: rastreia o [preço mais alto](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) durante os últimos 25 períodos.
  - **Lowest 45**: monitora o [preço mais baixo](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) durante os últimos 45 períodos.
- **Lógica de Cálculo**: determina os pontos de execução de operações [comparando](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) os preços atuais com os níveis de máxima e mínima calculados pelos indicadores.

### Execução de Operações

- **Sinal de Entrada**: uma ordem de [compra](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) é iniciada quando o preço atual cruza [abaixo]() do ponto mínimo calculado pelo indicador "Lowest 45".
- **Sinal de Saída**: uma ordem de [venda](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) é acionada quando a ação do preço subsequente não sustenta a continuação da tendência de baixa, definida por parâmetros de cálculo específicos.

### Visualização

- **Exibição no Gráfico**: os valores dos indicadores "Highest 25" e "Lowest 45" são plotados no [gráfico](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html) junto com as velas de preço, fornecendo uma representação visual dos possíveis pontos de rompimento.

## Detalhes de Implementação

- **Plataforma**: implementada na plataforma StockSharp, utilizando suas capacidades para processamento de dados em tempo real e cálculo de indicadores.
- **Uso de Indicadores**: emprega indicadores de máxima e mínima para estabelecer um intervalo dentro do qual a estratégia busca pontos de rompimento.

## Conclusão

A estratégia "Rompimento de Mínimas com Cálculo" foi desenvolvida para traders que buscam oportunidades baseadas em rompimentos de preços a partir de máximas ou mínimas estabelecidas. Esta estratégia combina indicadores técnicos com uma lógica de cálculo sofisticada para identificar e agir sobre possíveis movimentos de mercado.
