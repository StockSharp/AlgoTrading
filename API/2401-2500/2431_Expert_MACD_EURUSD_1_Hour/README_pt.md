# Estratégia Expert MACD EURUSD 1 Hora
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é uma tradução em C# do consultor especialista MetaTrader 5 **Expert MACD EURUSD 1 Hour**. Opera em velas de uma hora usando o indicador MACD com períodos curto, longo e de sinal de **5 / 15 / 3**. A estratégia procura uma forte mudança de momentum onde a linha principal do MACD cruza acima ou abaixo do nível zero enquanto a linha de sinal confirma o movimento. Um trailing stop é usado para proteger as posições abertas, e as negociações são fechadas quando a inclinação do MACD se volta contra a posição atual.

## Parâmetros

- `FastLength` – período da EMA rápida para MACD (padrão: 5).
- `SlowLength` – período da EMA lenta para MACD (padrão: 15).
- `SignalLength` – período da linha de sinal para MACD (padrão: 3).
- `TrailingPoints` – distância do trailing stop em pontos de preço (padrão: 25).
- `CandleType` – período das velas (padrão: 1 hora).
- A propriedade `Volume` da estratégia controla o tamanho da ordem.

## Lógica de negociação

### Entrada comprada
1. Valores da linha de sinal: `mac8 > mac7 > mac6` e `mac6 < mac5` (linha de sinal subindo).
2. Valores da linha principal: `mac4 > mac3 < mac2 < mac1` (linha principal subindo após uma queda).
3. `mac2 < -0.00020`, `mac4 < 0` e `mac1 > 0.00020` – linha principal cruza acima de zero.
4. Se todas as condições forem satisfeitas e nenhuma posição comprada estiver aberta, comprar a mercado.

### Entrada vendida
1. Valores da linha de sinal: `mac8 < mac7 < mac6` e `mac6 > mac5` (linha de sinal caindo).
2. Valores da linha principal: `mac4 < mac3 > mac2 > mac1` (linha principal caindo após um pico).
3. `mac2 > 0.00020`, `mac4 > 0` e `mac1 < -0.00035` – linha principal cruza abaixo de zero.
4. Se todas as condições forem satisfeitas e nenhuma posição vendida estiver aberta, vender a mercado.

### Regras de saída
- Fechar uma compra quando o valor principal atual está abaixo do anterior.
- Fechar uma venda quando o valor principal atual está acima do anterior.
- O trailing stop se atualiza a cada vela e sai se o preço cruzar o nível de stop.

## Notas

Este exemplo demonstra o uso da API de alto nível do StockSharp com vinculação de indicadores e gerenciamento manual de trailing stop. É destinado a fins educacionais e não inclui gerenciamento de dinheiro além do parâmetro fixo `Volume`.
