# Estratégia Adaptive Renko
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia constrói uma grade Renko adaptativa onde o tamanho do tijolo segue a volatilidade do mercado medida pelo indicador **Average True Range (ATR)**. Uma operação é executada sempre que o preço percorre um tijolo completo em qualquer direção.

## Lógica
- O ATR é calculado sobre um `VolatilityPeriod` configurável.
- O tamanho do tijolo é igual a `ATR * Multiplier`, mas não pode ser menor que `MinBrickSize`.
- Quando o preço sobe acima do tijolo anterior por pelo menos um tamanho de tijolo, a estratégia compra (fechando posições vendidas, se necessário).
- Quando o preço cai abaixo do tijolo anterior por pelo menos um tamanho de tijolo, a estratégia vende (fechando posições compradas, se necessário).

## Parâmetros
- `Volume` – volume da ordem.
- `VolatilityPeriod` – período usado para o ATR.
- `Multiplier` – coeficiente aplicado ao ATR.
- `MinBrickSize` – tamanho mínimo permitido do tijolo em unidades de preço.
- `CandleType` – período para o cálculo do ATR.

## Período
- Padrão: 4 horas.
