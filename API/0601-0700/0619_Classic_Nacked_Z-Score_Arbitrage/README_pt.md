# Estratégia Clássica de Arbitragem Z-Score Simples
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera o spread entre dois ativos usando o Z-Score. Quando o z-score do spread sobe acima de um limiar positivo, a estratégia vende o primeiro ativo e compra o segundo. Quando o z-score cai abaixo do limiar negativo, compra o primeiro ativo e vende o segundo. As posições são fechadas quando o z-score reverte em direção a zero.

## Parâmetros
- Tipo de candle
- Período de lookback
- Limiar Z-Score
- Segundo instrumento
