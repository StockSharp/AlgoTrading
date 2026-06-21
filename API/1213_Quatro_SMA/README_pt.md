# Estratégia Quatro SMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina três médias móveis simples (SMAs) rápidas com uma SMA de longo prazo e um filtro de volume. Uma posição comprada é aberta quando a SMA mais rápida está acima da SMA média, a média está acima da SMA lenta, o preço está acima da SMA longa e o volume supera sua média por um multiplicador configurável. Posições vendidas exigem o alinhamento oposto.

A posição é encerrada em várias etapas: até três níveis de take-profit e um stop-loss podem fechar partes da operação. Um alinhamento inverso das SMAs também fecha a posição.

## Detalhes

- **Indicadores**: SMA, Volume
- **Período**: 4h
- **Tipo**: Seguidor de tendência com confirmação de volume
- **Stops**: Três níveis de take-profit e um stop-loss
