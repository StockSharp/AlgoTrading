# Estratégia CoeffofLine True
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia porta o expert MQL5 `Exp_CoeffofLine_true.mq5` para o framework StockSharp. Rastreia a **Inclinação da Regressão Linear** dos preços medianos e reage aos cruzamentos de zero.

Uma posição comprada é aberta quando a inclinação se torna positiva após ser negativa. Uma posição vendida é aberta quando a inclinação se torna negativa após ser positiva. As posições existentes são fechadas em sinais opostos. Apenas velas completas são processadas.

## Parâmetros

- **Candle Type** – período para a série de velas.
- **Slope Period** – comprimento da regressão linear utilizada para calcular a inclinação.
- **Signal Bar** – índice de barra histórica utilizado para avaliação de sinais.
- **Buy Open / Sell Open** – permissões para abrir posições compradas ou vendidas.
- **Buy Close / Sell Close** – permissões para fechar posições compradas ou vendidas.

A estratégia subscreve velas, vincula o indicador através da API de alto nível e opera sem solicitações manuais de valores do indicador.
