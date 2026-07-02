# Exemplo da Estratégia High Break no StockSharp Strategy Designer
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia "High Break" representada no esquema JSON fornecido foi projetada para executar negociações com base em condições específicas relacionadas a movimentos de preço e intervalos de tempo, usando o StockSharp Strategy Designer. Este exemplo demonstra como configurar uma estratégia de negociação que identifica potenciais oportunidades de compra quando o preço de um ativo ultrapassa uma máxima predeterminada ao longo de um determinado período.

![schema](schema.png)

## Descrição do esquema

O esquema descreve uma sequência de componentes interconectados projetados para capturar, analisar e agir sobre dados de mercado em tempo real:

1. **Nó Security**: Serve como base, especificando o [ativo](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) (por exemplo, ações, futuros) ao qual a estratégia é aplicada. Este nó é crítico, pois determina a entrada de dados para a estratégia.

2. **Nó TimeFrameCandle**: Processa os dados de mercado recebidos e os organiza em [candles com base](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) em um intervalo de tempo especificado. Este nó é vital para estratégias que dependem de análise histórica de preços para tomar decisões de negociação.

3. **Nó Highest**: Analisa os dados de candles para [determinar o preço mais alto](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) atingido ao longo de um período de tempo especificado (por exemplo, 60 minutos). Este valor estabelece uma referência para identificar rompimentos de preço significativos.

4. **Nó de comparação**: [Compara](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) os preços atuais com a máxima histórica determinada pelo nó Highest. Se o preço atual exceder esta máxima, aciona um potencial sinal de negociação.

5. **Nó Chart Panel**: [Visualiza](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html) os dados de preço e as ações da estratégia, fornecendo uma representação gráfica da operação da estratégia, o que auxilia no monitoramento e nos ajustes.

6. **Nós de execução de negociações (Compra/Venda)**: Responsáveis por [executar negociações](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) quando as condições da estratégia são atendidas. Por exemplo, uma ordem de compra pode ser executada quando o preço ultrapassa a máxima histórica.

## Fluxo de trabalho

- O **Nó Security** alimenta dados de mercado no **Nó TimeFrameCandle** para criar um conjunto de dados de candles estruturado com base no tempo.
- O **Nó Highest** calcula o preço mais alto a partir desses candles ao longo de um período definido.
- O **Nó de comparação** compara continuamente o preço atual com essa máxima. Se o preço atual exceder a máxima histórica, sugere uma ruptura de alta, potencialmente acionando um sinal de compra.
- O **Nó Chart Panel** fornece visualização em tempo real, permitindo feedback visual imediato sobre o desempenho da estratégia e as condições do mercado.
- Quando a condição de compra é atendida, o **Nó de execução de negociação** (Compra) inicia uma negociação, capitalizando o momentum ascendente esperado.

## Aplicação prática

Esta configuração é especialmente útil para traders especializados em estratégias de rompimento, onde reconhecer e agir sobre movimentos de preço acima de determinados limites pode levar a negociações rentáveis. Tais estratégias são populares em mercados voláteis, onde rompimentos de preço podem sinalizar tendências fortes.

## Conclusão

O exemplo da estratégia "High Break" no StockSharp Strategy Designer ilustra um uso sofisticado de dados de mercado para automatizar decisões de negociação com base em movimentos de preço identificados. Ao aproveitar ferramentas de processamento de dados em tempo real e visualização, a estratégia ajuda os traders a capitalizar eficientemente as oportunidades de mercado apresentadas por rompimentos de preço. Este exemplo não apenas demonstra o poder da plataforma StockSharp no desenvolvimento de estratégias de negociação dinâmicas, mas também serve como base para maior personalização e otimização com base em requisitos individuais de negociação e condições de mercado.
