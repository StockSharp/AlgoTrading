# Estratégia de Grade de Três Níveis
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa um sistema de trading em grade simétrica com até três classificações de take-profit.
Ordens limite são colocadas acima e abaixo do preço atual em intervalos fixos. Quando uma ordem
de entrada é executada, uma ordem limite oposta é enviada para capturar lucro a uma distância configurável.
O método é adequado para mercados laterais onde o preço oscila dentro de uma faixa.

## Parâmetros

- `Grid Size` – distância entre os níveis da grade.
- `Levels` – número de níveis da grade em cada lado do preço atual.
- `Base Take Profit` – distância de lucro base para o primeiro ranking.
- `Order Volume` – volume usado para cada ordem da grade.
- `Enable Rank1` – colocar ordens com take-profit base.
- `Enable Rank2` – colocar ordens com take-profit base mais um tamanho de grade.
- `Enable Rank3` – colocar ordens com take-profit base mais dois tamanhos de grade.
- `Allow Longs` – habilitar o lado comprado da grade.
- `Allow Shorts` – habilitar o lado vendido da grade.
- `Candle Type` – tipo de candle usado para obter o preço de referência inicial.

## Lógica de Trading

1. No início, a estratégia assina candles e aguarda o primeiro candle concluído.
2. Usando o preço de fechamento desse candle, a grade é construída com o número configurado de níveis.
3. Para cada nível, ordens limite de compra e/ou venda são colocadas dependendo dos lados permitidos.
4. Quando uma ordem de entrada é executada, uma ordem limite oposta é registrada no preço de take-profit
   calculado com base no ranking selecionado.
5. As ordens permanecem no mercado até serem executadas ou canceladas manualmente.

Esta implementação é uma conversão simplificada do sistema de grade MQL original e tem como objetivo
destacar a mecânica central na API de alto nível do StockSharp.
