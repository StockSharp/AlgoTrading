# Descrição da Estratégia Bollinger Bands
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral da estratégia

A estratégia "Bollinger Bands" foi projetada para o [StockSharp Designer](https://doc.stocksharp.com/topics/designer.html) e concentra-se em utilizar as Bollinger Bands para capitalizar padrões de volatilidade. Esta estratégia detecta o cruzamento do preço com as bandas para determinar os pontos de entrada e saída no mercado.

![schema](schema.png)

## Detalhes da estratégia

### Componentes

1. **Formação de candles**: Usa um intervalo de cinco minutos para gerar [candles](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) e aciona a análise quando cada candle fecha.
2. **Indicador Bollinger Bands**: Calcula as bandas superior e inferior das [Bollinger Bands](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) com período de 32 e multiplicador de desvio padrão de 2.0.
3. **Sinais de negociação**:
   - **Sinal de compra**: Um sinal de compra é gerado quando o [preço mínimo](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) do candle [cruza](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/crossing.html) abaixo da banda inferior das Bollinger Bands, sugerindo uma condição de sobrevenda.
   - **Sinal de venda**: Um sinal de venda é acionado quando o [preço máximo](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) do candle [cruza](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/crossing.html) acima da banda superior das Bollinger Bands, indicando uma condição de sobrecompra.

### Execução de negociações

- **Tipo de ordem**: [Ordens a mercado](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) são usadas tanto para entrada quanto para saída, a fim de garantir execução rápida.
- **Gestão de posições**: As posições são abertas com base nos sinais de cruzamento e fechadas em um cruzamento na direção oposta ou com base em condições predefinidas de stop-loss ou take-profit.

### Gestão de risco

- **Stop-Loss e Take-Profit**: Configurações ajustáveis permitem níveis fixos ou baseados em percentual de [stop-loss e take-profit](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/protect_position.html) para gerenciar o risco de forma eficaz.
- **Gestão de capital**: A estratégia inclui parâmetros para ajustar o tamanho das negociações com base no saldo disponível da conta e nos níveis de risco.

## Conclusão

A estratégia "Bollinger Bands" fornece uma abordagem sistemática para negociação baseada em volatilidade e condições de mercado, tornando-a adequada para traders que buscam um sistema de negociação automatizado e robusto na plataforma StockSharp. Ela combina indicadores técnicos com regras precisas de execução de negociações para aprimorar o desempenho em diferentes ambientes de mercado.
