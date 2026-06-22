# Estratégia CrossMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera um cruzamento simples de médias móveis com um stop loss baseado em ATR. Uma posição comprada é aberta quando a SMA rápida cruza acima da SMA lenta. Uma posição vendida é aberta quando a SMA rápida cruza abaixo da SMA lenta. Após entrar em uma posição, um stop loss é colocado a uma distância de um ATR do preço de entrada e é verificado em cada nova vela.

## Parâmetros
- Tipo de vela
- Período da SMA rápida
- Período da SMA lenta
- Período do ATR
- Volume
