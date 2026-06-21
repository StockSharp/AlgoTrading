# Estratégia de Cruzamento EMA com Filtros
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa múltiplas médias móveis exponenciais (EMAs) para operar cruzamentos com filtros de tendência adicionais.

A estratégia compra quando a EMA de 100 cruza acima da EMA de 200 enquanto a EMA de 9 está acima da EMA de 50. Vende a descoberto quando a EMA de 100 cruza abaixo da EMA de 200 e a EMA de 9 está abaixo da EMA de 50. As posições compradas saem quando a EMA de 100 cruza abaixo da EMA de 50; as posições vendidas saem quando a EMA de 100 cruza acima da EMA de 50.

## Parâmetros
- Tipo de vela
- Comprimento da EMA 9
- Comprimento da EMA 50
- Comprimento da EMA 100
- Comprimento da EMA 200
