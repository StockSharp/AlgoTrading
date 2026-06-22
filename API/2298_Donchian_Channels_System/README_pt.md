# Sistema de Canais Donchian
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Sistema de Canais Donchian** opera rompimentos do Canal Donchian com um deslocamento opcional para evitar viés de antecipação.

## Como Funciona
- **Entrada comprada**: quando o preço de fechamento cruza acima da banda superior de Donchian calculada `Shift` barras atrás.
- **Entrada vendida**: quando o preço de fechamento cruza abaixo da banda inferior de Donchian calculada `Shift` barras atrás.
- As posições são revertidas no rompimento oposto.

## Parâmetros
- `DonchianPeriod` = 20
- `Shift` = 2
- `CandleType` = 4h

## Indicadores
- Canal Donchian
