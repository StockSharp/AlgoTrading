# Estratégia de Scalping Agressivo Simples com MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Implementa uma estratégia de scalping usando o histograma do MACD com um filtro EMA de 50 períodos.

- Vai comprado quando o histograma do MACD cruza acima de zero e o preço está acima da EMA.
- Vai vendido quando o histograma cruza abaixo de zero e o preço está abaixo da EMA.
- Fecha posições quando o momentum do histograma se reverte.
