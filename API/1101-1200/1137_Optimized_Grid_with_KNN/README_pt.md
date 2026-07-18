# Estratégia de Grade Otimizada com KNN
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia abre posições compradas quando a linha T3 rápida cruza acima da linha T3 lenta e a variação média de preço baseada em KNN é positiva. Os limites de entrada e saída são ajustados pela variação média. As posições são fechadas assim que a linha T3 rápida cruza abaixo da lenta e o preço supera o limite de lucro.

- **Condições de entrada**: `t3Fast > t3Slow` e `averageChange > 0`
- **Condições de saída**: `t3Fast < t3Slow` e `(close - lastEntryPrice)/lastEntryPrice > adjustedCloseTh`
- **Indicadores**: T3
