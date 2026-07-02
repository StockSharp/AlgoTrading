# Exemplo de Tratamento de Profundidade de Mercado no StockSharp Strategy Designer
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral

Este exemplo ilustra uma configuração dentro do StockSharp Strategy Designer focada no tratamento de dados de profundidade de mercado. Os dados de profundidade de mercado, frequentemente chamados de "livro de ordens", incluem informações sobre ordens de compra e venda em diferentes níveis de preço para um ativo. São fundamentais para estratégias que precisam analisar dinâmicas de oferta e demanda em vários níveis de preço em tempo real.

![schema](schema.png)

## Descrição do Esquema

O esquema é composto por vários componentes interconectados projetados para buscar, processar e exibir informações de profundidade de mercado:

1. **Nó de Instrumento**: este nó representa o [ativo](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) (por exemplo, uma ação, futuro ou outro instrumento financeiro) para o qual a profundidade de mercado será obtida. É um elemento fundamental, pois define qual mercado ou instrumento está sendo analisado.

2. **Nó TimeFrameCandle**: lida com os [dados de velas](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) do ativo, agregados em um intervalo de tempo especificado (5 minutos no exemplo). Pode ser usado para correlacionar mudanças na profundidade de mercado com movimentos de preços ao longo do tempo.

3. **Nós de Profundidade de Mercado**: são projetados para capturar e possivelmente reagir a mudanças em tempo real na [profundidade de mercado](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/market_depths/order_book.html). Inclui configurações para processar dados de profundidade de mercado recebidos, fornecendo informações sobre as ordens de compra e venda atuais.

4. **Nó do Painel de Gráfico**: indica que os dados de velas são visualizados em um [gráfico](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html). Isso pode ajudar traders ou algoritmos a visualizar melhor a situação do mercado e tomar decisões fundamentadas.

5. **Nó do Painel de Profundidade de Mercado**: focado especificamente na exibição dos dados de profundidade de mercado em um [painel especial](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/market_depths/order_book_panel.html), com funcionalidades como destaque dos melhores preços de compra e venda e visualização da profundidade de mercado.

## Fluxo de Trabalho

- O **Nó de Instrumento** fornece dados que são usados como entrada tanto para o **Nó TimeFrameCandle** quanto para o **Nó de Profundidade de Mercado**.
- O **Nó TimeFrameCandle** processa esses dados para gerar velas no intervalo de tempo especificado, que podem ser usadas para análise de tendências ou outros fins de análise técnica.
- O **Nó de Profundidade de Mercado** processa a profundidade de mercado em tempo real do ativo especificado. Pode ser usado para acionar decisões de trading com base em condições específicas, como um grande desequilíbrio entre ordens de compra e venda em determinados níveis de preço.
- A visualização ocorre por meio do **Nó do Painel de Gráfico** e do **Nó do Painel de Profundidade de Mercado**, garantindo que os dados não sejam apenas processados para a lógica de trading, mas também tornados acessíveis para revisão.

## Aplicação Prática

Esta configuração pode ser usada em uma variedade de estratégias de trading, incluindo:
- **Trading de Alta Frequência (HFT)**, onde pequenas mudanças na dinâmica do livro de ordens podem indicar negociações potencialmente lucrativas.
- **Estratégias de Arbitragem**, que podem envolver a comparação de livros de ordens em múltiplas exchanges para explorar discrepâncias de preços.
- **Estratégias de Market Making**, onde entender ambos os lados do livro de ordens é fundamental para definir ordens de compra e venda adequadas.

## Conclusão

O esquema fornecido no arquivo JSON demonstra uma abordagem abrangente ao tratamento de dados de profundidade de mercado no StockSharp Strategy Designer. Ao integrar processamento de dados em tempo real com sofisticadas ferramentas de visualização, esta configuração auxilia traders e algoritmos a tomar decisões rápidas e baseadas em dados sobre o estado do livro de ordens. Este exemplo serve como uma base sólida para desenvolver estratégias de trading mais complexas que exigem insights profundos sobre a dinâmica do mercado.
