# Estratégia de Confirmação de Tendência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina SuperTrend, MACD e VWAP para confirmar tendências.

## Detalhes
- **Critérios de entrada**: Direção do SuperTrend com confirmação MACD e preço relativo ao VWAP.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: MACD cruzando sua linha de sinal contra a posição.
- **Stops**: Nenhum.
- **Valores padrão**: Comprimento ATR 10, Fator 3, MACD rápido 12, lento 26, sinal 9.
- **Filtros**: SuperTrend e VWAP.
