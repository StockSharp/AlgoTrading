# Exemplo de Tratamento de Data e Hora no StockSharp Strategy Designer
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral

Este exemplo no StockSharp Strategy Designer demonstra uma configuração sofisticada que integra o tratamento de data e hora dentro de uma estratégia de trading. A estratégia utiliza condições específicas de tempo para tomar decisões de negociação com base nos dados de velas e na hora do dia, tornando-o um exemplo prático para cenários onde as operações são sensíveis ao tempo.

![schema](schema.png)

## Descrição do Esquema

O esquema apresentado no arquivo JSON descreve uma interação complexa entre vários nós que lidam com dados baseados em tempo para acionar ações de trading:

1. **Nó TimeFrameCandle**: processa os [dados de velas](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) para um intervalo de tempo especificado. É fundamental para estratégias que dependem de movimentos históricos de preços para prever tendências futuras.

2. **Nós OpenTime e CloseTime**: [extraem](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) os horários de abertura e fechamento dos dados de velas, que são fundamentais para determinar os períodos específicos durante os quais as condições de trading são avaliadas.

3. **Nós de Comparação (Equals, Greater Than)**: [comparam](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) horários específicos (como 14:00:00 ou 15:00:00) com o horário atual extraído dos dados de velas. Esta configuração permite que a estratégia seja ativada ou desativada com base no horário especificado.

4. **Nó do Painel de Gráfico**: implementa [componentes de visualização](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html) que exibem dados de trading e indicadores em um formato compreensível, auxiliando na tomada de decisões em tempo real e nos ajustes da estratégia.

5. **Nós de Trading (Compra, Venda)**: são ativados quando certas condições de tempo são atendidas, permitindo que a estratégia execute [ordens de compra ou venda](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) com base nos resultados de comparação e na lógica de trading definida na estratégia.

## Fluxo de Trabalho

- O **Nó TimeFrameCandle** coleta e processa dados de velas em intervalos regulares.
- Os **Nós OpenTime e CloseTime** analisam esses dados para extrair pontos de tempo específicos.
- Os **Nós de Comparação** verificam esses horários contra valores predefinidos (por exemplo, 14:00:00 para uma condição de entrada e 15:00:00 para uma condição de saída).
- Quando as condições são atendidas (por exemplo, o horário atual é igual a 14:00:00), os nós de trading (Compra ou Venda) são acionados para executar operações com base na lógica da estratégia.
- O **Nó do Painel de Gráfico** representa visualmente essas operações e os dados de velas, fornecendo uma visão clara do funcionamento da estratégia e das condições de mercado.

## Aplicação Prática

Esta configuração é particularmente útil para estratégias que precisam executar operações em horários específicos do dia, tais como:
- **Rompimentos do Range de Abertura**, onde as operações são colocadas em torno da abertura de uma sessão de mercado.
- **Estratégias de Leilão de Fechamento**, visando movimentos de preços e variações de liquidez que ocorrem no fechamento da sessão de trading.

## Conclusão

Este exemplo do StockSharp Strategy Designer ilustra um framework robusto para desenvolver estratégias de trading sensíveis ao tempo que podem executar operações automaticamente em horários predefinidos. É uma excelente demonstração de como os traders podem aproveitar as capacidades do Strategy Designer para criar estratégias de trading complexas e baseadas em regras que respondem dinamicamente aos dados do mercado em tempo real e a condições temporais específicas.
