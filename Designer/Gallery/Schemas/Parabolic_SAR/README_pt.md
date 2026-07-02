# Descrição da Estratégia Parabolic SAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral da Estratégia

A estratégia "Parabolic SAR" foi desenvolvida para capturar reversões e padrões de continuação de tendência usando o indicador Parabolic Stop and Reverse (SAR) dentro do [StockSharp Designer](https://doc.stocksharp.com/topics/designer.html). Esta estratégia fornece sinais claros de entrada e saída com base no movimento do preço em relação aos pontos do Parabolic SAR.

![schema](schema.png)

## Detalhes da Estratégia

### Componentes

- **Formação de Velas**: utiliza um intervalo de tempo de 5 minutos para [analisar a ação do preço](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html), garantindo que a estratégia capture os movimentos de mercado de curto prazo de forma eficaz.
- **Indicador Parabolic SAR**: [configurado](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) com um fator de aceleração inicial de 0,02, um passo de aceleração de 0,02 e uma aceleração máxima de 0,2. Essas configurações permitem que o indicador se adapte à volatilidade do mercado.

### Execução de Operações

- **Sinal de Entrada**: um sinal de compra é gerado quando o preço cruza [acima](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) dos pontos do Parabolic SAR, indicando uma possível tendência de alta.
- **Sinal de Saída**: um sinal de venda é emitido quando o preço cai [abaixo](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) dos pontos do Parabolic SAR, sugerindo uma possível tendência de baixa.

### Visualização

- **Exibição no Gráfico**: os pontos do Parabolic SAR são plotados no [gráfico](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html) junto com as velas de preço, fornecendo uma representação visual da tendência e dos possíveis sinais de trading.

## Detalhes de Implementação

- **Plataforma**: implementada na plataforma StockSharp, aproveitando seus recursos abrangentes para obtenção de dados em tempo real, cálculo de indicadores e execução de operações.
- **Aplicação do Indicador**: o Parabolic SAR é aplicado diretamente ao gráfico de preços, permitindo uma avaliação visual imediata das mudanças de tendência e da validade das configurações de trading.

## Conclusão

A estratégia "Parabolic SAR" é ideal para traders que precisam de sinais de trading precisos e automáticos baseados em padrões de reversão de tendência. Ela aproveita a natureza dinâmica do Parabolic SAR para fornecer entradas e saídas oportunas, aumentando o potencial de lucro em mercados de rápido movimento.
