# Estratégia de Negociação por Pares em BTC e ETH
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A Estratégia de Negociação por Pares em BTC e ETH foi projetada para negociar duas criptomoedas populares — Bitcoin (BTC) e Ethereum (ETH). Esta estratégia de arbitragem de criptomoedas baseia-se na identificação de oportunidades de arbitragem entre esses dois ativos, permitindo que os traders capitalizem momentos em que a diferença de preço entre BTC e ETH atinge um determinado limiar.

![schema](schema.png)

A estratégia implementa mecanismos para comprar uma criptomoeda enquanto simultaneamente vende a outra, visando lucrar com discrepâncias temporárias em seus valores. Isso torna a estratégia atraente para aqueles que buscam oportunidades de ganho a partir de flutuações mínimas do mercado sem estar vinculados à tendência geral do mercado.

## Instalação

Para ativar e usar esta estratégia, o StockSharp Designer deve estar instalado. A estratégia está disponível para download e instalação na [galeria de estratégias](https://doc.stocksharp.com/topics/designer/strategy_gallery.html). Isso permite fácil integração e personalização da estratégia de acordo com os requisitos individuais do trader.

## Parâmetros

- **Ativo 1**: ETHUSDT@BNB
- **Ativo 2**: BTCUSDT@BNB
- **Limiar**: 0.02 (absoluto)
- **Volume de Negociação**: 5000 (absoluto)
- **Slippage**: 1.0 (absoluto)
- **Máximo de Ordens**: 3 (absoluto)

## Como funciona

1. **Coleta de dados de preço**: A estratégia coleta dados de preço de BTC e ETH da exchange Binance.
2. **Cálculo de preço**: Calcula a relação de preços entre BTC e ETH.
3. **Geração de sinais**: Quando a relação de preços excede o limiar definido, a estratégia gera sinais de compra e venda.
4. **Execução de ordens**: A estratégia executa ordens a mercado para comprar o ativo subvalorizado e vender o ativo sobrevalorizado.
5. **Cálculo de lucro**: Calcula o lucro com base nas negociações executadas e monitora o mercado em busca de mais oportunidades.

## Testes

É importante testar a estratégia em dados históricos para avaliar sua eficácia e riscos potenciais antes de aplicá-la no mercado real. Isso ajudará a determinar os parâmetros ótimos para o limiar de discrepâncias de preço e gestão de capital.

![profit](profit.png)

## Recursos adicionais

Para mais informações e recursos, visite a [documentação do StockSharp](https://doc.stocksharp.com/).
