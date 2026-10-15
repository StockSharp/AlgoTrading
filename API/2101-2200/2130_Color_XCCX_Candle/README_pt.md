# Estratégia Color XCCX Candle
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Convertida a partir do código MQL `MQL/14260`.

Esta estratégia compara duas médias móveis simples (SMA) construídas a partir dos preços de abertura e fechamento das velas. Quando a SMA calculada a partir dos preços de fechamento cruza acima da SMA baseada nos preços de abertura, uma posição comprada é aberta. Quando a SMA baseada no fechamento cruza abaixo da SMA baseada na abertura, uma posição vendida é aberta. Qualquer posição oposta existente é fechada antes de abrir uma nova.

Parâmetros:

- `SMA Length` – número de velas utilizadas para calcular ambas as SMAs.
- `Candle Type` – período para as velas recebidas.
- `Stop Loss %` – tamanho do stop loss como percentual do preço de entrada.
- `Take Profit %` – tamanho do take profit como percentual do preço de entrada.

A estratégia usa a API de alto nível do StockSharp para se inscrever em velas e vincular indicadores. Também traça ambas as SMAs e as negociações executadas no gráfico quando a visualização está disponível.
