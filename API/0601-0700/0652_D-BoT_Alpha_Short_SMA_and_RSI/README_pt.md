# Estratégia D-BoT Alpha Short SMA e RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia vendida que vende quando o RSI cruza acima de um limiar enquanto o preço permanece abaixo de uma média móvel simples. Um trailing stop acompanha novos mínimos e as posições são fechadas se o RSI atingir os níveis de stop ou take-profit.

## Detalhes

- **Critérios de entrada**: O RSI cruza acima do nível de entrada e o preço está abaixo da SMA.
- **Critérios de saída**: O preço cruza acima do trailing stop ou o RSI atinge os níveis de stop ou take-profit.
