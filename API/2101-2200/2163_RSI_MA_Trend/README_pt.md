# Estratégia de Tendência RSI MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina o Índice de Força Relativa (RSI) com um filtro de tendência de média móvel.
Uma posição comprada é aberta quando o RSI cai abaixo de um nível de compra especificado enquanto a média móvel rápida está acima da média móvel lenta.
Uma posição vendida é aberta quando o RSI sobe acima de um nível de venda especificado enquanto a média móvel rápida está abaixo da média móvel lenta.

## Parâmetros

- `RSI Period` – comprimento do indicador RSI.
- `RSI Buy Level` – valor do RSI abaixo do qual uma posição comprada é aberta.
- `RSI Sell Level` – valor do RSI acima do qual uma posição vendida é aberta.
- `Fast MA Period` – período da média móvel rápida.
- `Slow MA Period` – período da média móvel lenta.
- `Candle Type` – série de velas utilizada para os cálculos.

## Lógica

1. Assinar a série de velas selecionada.
2. Calcular RSI, MA rápida e MA lenta para cada vela finalizada.
3. Detectar tendência de alta quando a MA rápida está acima da MA lenta e tendência de baixa quando está abaixo.
4. Entrar comprado quando RSI < nível de compra e a tendência é de alta, fechando posições vendidas se houver.
5. Entrar vendido quando RSI > nível de venda e a tendência é de baixa, fechando posições compradas se houver.

## Notas

- A estratégia utiliza ordens a mercado para entradas.
- Os sinais de operação são processados apenas em velas finalizadas.
- Os parâmetros estão disponíveis para otimização na interface do usuário.
