# Estratégia de Cruzamento TRIX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza dois indicadores TRIX (Oscilador de Média Móvel Tripla Exponencial) com diferentes períodos para detectar potenciais reversões. Uma posição comprada é aberta quando o TRIX rápido forma um fundo local enquanto o TRIX lento está subindo. Uma posição vendida é aberta quando o TRIX rápido forma um topo local enquanto o TRIX lento está caindo.

## Parâmetros

- **Fast TRIX Period** – período do indicador TRIX rápido.
- **Slow TRIX Period** – período do indicador TRIX lento.
- **Take Profit** – objetivo de lucro em unidades de preço absolutas.
- **Stop Loss** – perda máxima em unidades de preço absolutas.
- **Candle Type** – período ou tipo de dados para as velas.

## Lógica de Trading

1. Subscrever o tipo de vela selecionado.
2. Calcular os valores de TRIX rápido e lento em cada vela finalizada.
3. Entrar comprado quando o valor do TRIX rápido é maior que seu valor anterior, o valor anterior é menor que o valor antes dele, e o TRIX lento está subindo.
4. Entrar vendido quando o valor do TRIX rápido é menor que seu valor anterior, o valor anterior é maior que o valor antes dele, e o TRIX lento está caindo.
5. Apenas uma posição é mantida de cada vez.
6. As proteções de stop loss e take profit são aplicadas automaticamente.

## Observações

A estratégia é uma adaptação de um script MQL5 e demonstra como trabalhar com indicadores TRIX dentro do StockSharp.
