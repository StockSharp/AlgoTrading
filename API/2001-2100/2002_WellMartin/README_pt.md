# Estratégia WellMartin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral

A estratégia **WellMartin** é um sistema de reversão à média que combina Bandas de Bollinger e o Índice Direcional Médio (ADX). Entra em posições compradas quando o preço rompe abaixo da banda inferior de Bollinger durante baixa força de tendência, e entra em posições vendidas quando o preço rompe acima da banda superior sob as mesmas condições. As posições são fechadas quando o preço atinge a banda oposta ou atinge os níveis configurados de take profit ou stop loss.

## Parâmetros

- **CandleType** – série de candles usada para cálculos.
- **BollingerPeriod** – período para as Bandas de Bollinger.
- **BollingerWidth** – multiplicador de desvio padrão para as Bandas de Bollinger.
- **AdxPeriod** – período para o indicador ADX.
- **AdxLevel** – limiar do ADX; negociações são realizadas apenas quando o valor ADX está abaixo deste nível.
- **Volume** – volume de negociação para cada entrada.
- **TakeProfit** – alvo de lucro em unidades de preço.
- **StopLoss** – limite de perda em unidades de preço.

## Lógica

1. Assinar dados de candles e calcular as Bandas de Bollinger e ADX.
2. Quando não há posição aberta:
   - **Comprar** se o preço de fechamento estiver abaixo da banda inferior e o ADX estiver abaixo do limiar.
   - **Vender** se o preço de fechamento estiver acima da banda superior e o ADX estiver abaixo do limiar.
3. Rastrear o lado da última negociação executada e permitir entradas apenas na mesma direção ou quando nenhuma negociação foi realizada.
4. Quando em uma posição comprada:
   - Sair se o preço tocar a banda superior, atingir o take profit ou o stop loss.
5. Quando em uma posição vendida:
   - Sair se o preço tocar a banda inferior, atingir o take profit ou o stop loss.

## Notas

Esta implementação usa um volume de negociação fixo. A versão MQL original aumentava o volume após uma negociação perdedora; esse comportamento pode ser adicionado mais tarde, se necessário.
