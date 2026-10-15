# Estratégia MESA Stochastic Multi Length
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza quatro osciladores MESA Stochastic com diferentes comprimentos de retrospectiva. Uma posição comprada é aberta quando todos os quatro osciladores estão acima de seu gatilho de média móvel. Uma posição vendida é aberta quando todos os quatro osciladores caem abaixo de seus gatilhos.

## Parâmetros
- `Length1` – retrospectiva do primeiro oscilador.
- `Length2` – retrospectiva do segundo oscilador.
- `Length3` – retrospectiva do terceiro oscilador.
- `Length4` – retrospectiva do quarto oscilador.
- `TriggerLength` – período de suavização para as médias móveis gatilho.
- `CandleType` – período de tempo das velas.
