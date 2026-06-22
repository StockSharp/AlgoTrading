# Estratégia Ride Alligator Williams
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa o indicador Alligator de Bill Williams. As linhas de lábios, dentes e mandíbula são calculadas a partir do preço mediano usando médias móveis suavizadas com comprimentos derivados de um período base através da razão áurea. Uma posição comprada é aberta quando os lábios cruzam acima da mandíbula enquanto os dentes permanecem abaixo. Uma posição vendida é aberta quando os lábios cruzam abaixo da mandíbula enquanto os dentes permanecem acima. Para uma posição aberta, um trailing stop segue a linha da mandíbula.

## Parâmetros
- **Base Period** – período raiz usado para derivar os comprimentos do Alligator.
- **Candle Type** – período das velas de entrada.

## Indicadores
- Média Móvel Suavizada (lábios, dentes, mandíbula)

## Regras de entrada
- Comprado quando os lábios cruzam acima da mandíbula e os dentes estão abaixo.
- Vendido quando os lábios cruzam abaixo da mandíbula e os dentes estão acima.

## Regras de saída
- Um cruzamento oposto fecha a posição.
- O trailing stop na linha da mandíbula sai quando o preço a cruza.
