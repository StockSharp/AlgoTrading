# Estratégia Exp Candles XSmoothed
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia monitora as máximas e mínimas dos candles suavizadas por uma média móvel ponderada (WMA). Quando o preço de fechamento rompe acima da máxima suavizada mais um buffer configurável, abre uma posição comprada e fecha qualquer posição vendida existente. Da mesma forma, um fechamento abaixo da mínima suavizada menos o buffer abre uma posição vendida e fecha qualquer posição comprada existente.

## Parâmetros
- **MA Length** – número de períodos para as médias móveis ponderadas aplicadas às máximas e mínimas.
- **Level** – buffer de rompimento em pontos adicionado à máxima suavizada e subtraído da mínima suavizada.
- **Candle Type** – período dos candles usados para análise.
- **Buy Open / Sell Open** – permissões para abrir posições compradas ou vendidas.
- **Buy Close / Sell Close** – permissões para fechar posições existentes quando ocorre um rompimento oposto.

A estratégia desenha linhas de máximas e mínimas suavizadas no gráfico para confirmação visual e usa proteção de posição integrada assim que iniciada.
