# Esquema de Estratégia de Negociação em Pares
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este esquema apresenta uma estratégia de negociação em pares baseada no valor relativo de dois títulos. Incorpora uma abordagem única para identificar e capitalizar as discrepâncias de preço entre dois ativos correlacionados.

## Visão Geral

A negociação em pares é uma estratégia neutra em relação ao mercado que envolve comprar um ativo e simultaneamente vender outro quando sua razão de preço se desvia da norma histórica. Este esquema usa o exemplo de dois títulos específicos: SBER@TQBR e GAZP@TQBR.

## Lógica da Estratégia

- **Cálculo do Índice**: A estratégia calcula um índice com base na fórmula `SBER@TQBR / GAZP@TQBR`. Este índice ajuda a determinar a força ou fraqueza relativa de uma ação em comparação com a outra.
- **Condição de Compra**: Se o índice sobe, indicando que SBER@TQBR está ficando mais caro em relação à GAZP@TQBR, a estratégia compra o ativo mais barato (GAZP@TQBR) e vende o mais caro (SBER@TQBR).
- **Condição de Venda**: Se o índice cai, sugerindo que SBER@TQBR está ficando mais barato em relação à GAZP@TQBR, a estratégia compra o ativo mais caro (SBER@TQBR) e vende o mais barato (GAZP@TQBR).

## Características Principais

- **Valores Arredondados**: Utiliza o operador `round` para converter os valores do índice calculados em inteiros. Essa simplificação auxilia na tomada de decisão ao fornecer sinais mais claros e acionáveis.
- **Neutralidade de Mercado**: Visa lucrar com a convergência da razão de preços em direção à sua média histórica, independentemente da direção geral do mercado.

## Aplicação e Benefícios

- **Mitigação de Risco**: Ao negociar em pares historicamente correlacionados, a estratégia minimiza o risco de mercado, pois os ganhos de um lado frequentemente compensam as perdas do outro.
- **Aproveitamento de Ineficiências de Preço**: A estratégia aproveita ineficiências temporárias nos preços dos títulos emparelhados, que se espera que eventualmente revertam para sua média.

## Execução

- **Condições de Configuração**: Antes de implementar a estratégia, certifique-se de que ambos os títulos sejam monitorados de perto para detectar divergências significativas que possam desencadear negociações.
- **Dinâmica Operacional**: O monitoramento contínuo e a recalibração dos níveis de limiar para compra e venda com base em dados históricos e condições de mercado são cruciais para o sucesso da estratégia.

O esquema apresentado não apenas descreve um framework robusto para negociação em pares, mas também destaca a importância de ferramentas matemáticas como o arredondamento na simplificação de decisões de negociação complexas.
