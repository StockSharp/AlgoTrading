# Estratégia SMI Correct
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia SMI Correct implementa um sistema de negociação baseado no indicador Stochastic Momentum Index (SMI). A estratégia monitora a linha SMI e sua linha de sinal de média móvel. Uma posição comprada é aberta quando o SMI cruza abaixo da linha de sinal. Uma posição vendida é aberta quando o SMI cruza acima da linha de sinal.

## Parâmetros
- **Candle Type** – período dos candles usados para os cálculos.
- **SMI Length** – número de períodos para o cálculo Estocástico.
- **Signal Length** – período de suavização para a linha de sinal.

## Como funciona
1. A estratégia subscreve candles do tipo especificado.
2. Para cada candle concluído, atualiza o oscilador Estocástico e a média móvel de sinal.
3. Quando o SMI cruza abaixo da linha de sinal, qualquer posição vendida é fechada e uma posição comprada é aberta.
4. Quando o SMI cruza acima da linha de sinal, qualquer posição comprada é fechada e uma posição vendida é aberta.

O exemplo também desenha candles e linhas do indicador em um gráfico para visualização.
