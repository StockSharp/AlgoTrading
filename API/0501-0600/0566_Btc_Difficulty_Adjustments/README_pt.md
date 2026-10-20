# BTC Difficulty Adjustments

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Regras de negociação

Exige `DifficultyHistoryFile` com cabeçalho `Time,Difficulty`. As publicações usam horários ISO UTC estritamente crescentes terminados em `Z`, com dificuldade positiva. O arquivo é lido ao iniciar; ausência ou formato inválido gera erro explícito. A dificuldade não é inferida dos preços.

Cada vela concluída consome somente observações publicadas até seu `OpenTime`. A primeira estabelece a base; valores iguais não geram sinal. Variação: `100*(difficulty/previous-1)`. Sem limiar, aumento compra e queda vende. Com limiar, atua somente `abs(change)>=Threshold`: queda compra e aumento vende. A última mudança observada admissível decide o lado. Abre `Volume` a partir de zero e reverte com `Volume+abs(Position)`. Ordens pendentes bloqueiam decisões sobrepostas. Não há stop automático.

## Parâmetros

| Parâmetro | Padrão |
|---|---|
| `CandleType` | `1d` |
| `ThresholdMode` | `false` |
| `Threshold` | `10` |
| `DifficultyHistoryFile` | `obrigatório` |
