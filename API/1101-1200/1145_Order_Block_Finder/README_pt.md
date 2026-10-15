# Estratégia de Localização de Blocos de Ordens
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia identifica blocos de ordens de alta e de baixa com base em um número especificado de velas consecutivas e um movimento percentual mínimo. Quando um bloco de ordens de alta é detectado, a estratégia compra; quando um bloco de baixa é encontrado, ela vende.

## Parâmetros
- **Relevant Periods** – número de velas subsequentes para confirmar um bloco de ordens
- **Min Percent Move** – variação percentual mínima entre o bloco e a última vela de confirmação
- **Use Whole Range** – usar o intervalo High/Low em vez dos limites baseados em Open
- **Candle Type** – tipo de vela utilizado para os cálculos
