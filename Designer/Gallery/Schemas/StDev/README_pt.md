# Descrição da StDevStrategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral da Estratégia

A "StDevStrategy" foi desenvolvida para o [StockSharp Designer](https://doc.stocksharp.com/topics/designer.html) para aproveitar os padrões de volatilidade estatística usando o indicador Standard Deviation. Esta estratégia é construída para identificar potenciais oportunidades de trading com base em desvios do preço médio, sinalizando condições de sobrecompra ou sobrevenda.

![schema](schema.png)

## Detalhes da Estratégia

### Componentes

- **Indicadores Standard Deviation**: utiliza múltiplos comprimentos para capturar a volatilidade de curto e longo prazo.
  - **Std Dev 20**: mede a volatilidade durante [20 períodos](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html).
  - **Lowest 15 e Highest 15**: rastreiam os valores mínimos e máximos durante 15 períodos para detectar condições de rompimento.
  - **Lowest 50**: captura mínimas de preços de longo prazo para avaliar condições de mercado estendidas.

### Execução de Operações

- **Tipo de Ordem**: executa operações usando [ordens a mercado](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) para garantir resposta rápida às mudanças de sinal.
- **Entrada e Saída**:
  - **Compra**: acionada quando a ação do preço sugere uma recuperação a partir de condições de sobrevenda.
  - **Venda**: iniciada quando a ação do preço indica uma possível queda a partir de condições de sobrecompra.
- **Gerenciamento de Posição**: emprega uma estratégia de dimensionamento dinâmico de posição que se ajusta com base na volatilidade do mercado e nos parâmetros de risco.

### Gerenciamento de Risco

- **Stop Loss e Take Profit**:
  - O [stop loss](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/protect_position.html) é definido em 1% abaixo da entrada para minimizar o risco.
  - O [take profit](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/protect_position.html) é definido em 2%, capturando potenciais altas enquanto protege os ganhos.

## Detalhes de Implementação

- **Plataforma**: implementada dentro da plataforma StockSharp aproveitando suas ferramentas abrangentes para análise de dados em tempo real e gerenciamento de ordens.
- **Indicadores Técnicos**: integra múltiplas instâncias de Standard Deviation junto com o rastreamento dos preços máximos e mínimos para melhorar a precisão do trading.

## Conclusão

A "StDevStrategy" é voltada para traders que preferem análise técnica e se concentram em capturar movimentos de preços impulsionados pela volatilidade. Ela fornece uma abordagem estruturada ao trading por meio do uso de indicadores avançados para gerenciar efetivamente os pontos de entrada e saída.
