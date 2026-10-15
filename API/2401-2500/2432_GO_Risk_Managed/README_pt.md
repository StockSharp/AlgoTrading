# Estratégia GO Risk Managed
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma portagem em C# do script original do MetaTrader "GO". Calcula um oscilador personalizado a partir de médias móveis dos preços de abertura, máximo, mínimo e fecho, e utiliza-o para determinar a direção do mercado.

## Lógica da Estratégia

1. São construídas quatro médias móveis com o mesmo período e método para as séries Open, High, Low e Close.
2. O valor *GO* é calculado a cada vela concluída:
   
   `GO = ((MA_close - MA_open) + (MA_high - MA_open) + (MA_low - MA_open) + (MA_close - MA_low) + (MA_close - MA_high)) * Volume`
3. Quando o valor GO se torna positivo, todas as posições vendidas são fechadas e uma nova posição comprada é aberta.
4. Quando o valor GO se torna negativo, todas as posições compradas são fechadas e uma nova posição vendida é aberta.
5. Apenas uma operação por barra é permitida. Novas entradas são realizadas até que o número total de posições abertas atinja **Max Positions**.

## Parâmetros

- **Risk %** – percentagem do capital da conta utilizada para calcular o volume da operação.
- **Max Positions** – número máximo de posições abertas permitidas numa direção.
- **MA Type** – tipo de média móvel (SMA, EMA, DEMA, TEMA, WMA, VWMA).
- **MA Period** – período para todas as médias móveis.
- **Candle Type** – série de velas utilizada para os cálculos dos indicadores.

## Notas

A implementação usa a API de alto nível do StockSharp. Subscreve velas, vincula indicadores e desenha-os no gráfico. O volume da operação é ajustado de acordo com a percentagem de risco especificada e os limites de volume do instrumento.
