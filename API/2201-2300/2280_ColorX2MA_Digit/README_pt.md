# Estratégia ColorX2MA Digit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é um port do especialista MQL5 **Exp_ColorX2MA_Digit**.
O algoritmo original pinta uma média móvel duplamente suavizada em cores diferentes dependendo da sua inclinação e usa essas cores para gerar sinais de negociação.
Nesta versão em C# o comportamento é aproximado por duas médias móveis simples e negocia nos seus cruzamentos.

## Lógica de negociação

- Uma média móvel **rápida** suaviza a série de preços.
- Uma média móvel **lenta** suaviza o resultado da rápida.
- Quando a média rápida cruza acima da média lenta, a estratégia abre uma posição comprada e fecha qualquer posição vendida existente.
- Quando a média rápida cruza abaixo da média lenta, a estratégia abre uma posição vendida e fecha qualquer posição comprada existente.
- Os sinais são processados apenas após o fechamento da vela.

## Parâmetros

- `FastLength` – comprimento do primeiro suavizamento (padrão 12).
- `SlowLength` – comprimento do segundo suavizamento (padrão 5).
- `CandleType` – período das velas usadas para cálculos.

A estratégia usa apenas a API de alto nível: `SubscribeCandles` com `Bind` para alimentar indicadores e `BuyMarket`/`SellMarket` para gerenciar posições. Os comentários no código estão em inglês para facilitar a manutenção.
