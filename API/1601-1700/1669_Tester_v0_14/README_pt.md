# Estratégia Tester v0.14
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia de exemplo é uma conversão simplificada do script MQL4 "Tester v0.14" originalmente projetado para EURUSD no período H4.

## Lógica

- Calcula uma média móvel simples de 14 períodos e MACD.
- Gera um sinal de compra quando o preço de fechamento está acima da SMA e o MACD é positivo.
- Gera um sinal de venda quando o preço de fechamento está abaixo da SMA e o MACD é negativo.
- Após a abertura de uma ordem, a posição é fechada após um número configurável de barras.

Esta conversão usa a API de alto nível do StockSharp, dependendo de `SubscribeCandles` e `Bind` para receber os valores do indicador.

## Parâmetros

- **MinSignSum** – número mínimo de sinais necessários para abrir uma posição.
- **Risk** – percentual do saldo da conta usado para gerenciamento de dinheiro.
- **TakeProfit / StopLoss** – níveis fixos em pontos.
- **BarsNumber** – número de barras para manter uma posição aberta.
- **CandleType** – série de candles utilizada (padrão: 4H).

## Notas

O arquivo MQL original continha centenas de combinações de regras. Este exemplo em C# ilustra a estrutura usando um conjunto reduzido de regras para maior clareza.
