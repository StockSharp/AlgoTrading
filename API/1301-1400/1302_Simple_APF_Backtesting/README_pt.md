# Backtesting de Estratégia APF Simples
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa um modelo simplificado de Previsão de Preços por Autocorrelação (APF). Detecta ciclos de preços via autocorrelação e prevê o preço futuro usando uma regressão linear dos retornos recentes. Uma posição comprada é aberta quando o ganho previsto supera um limiar especificado. A posição é fechada quando o preço alvo é atingido.

## Parâmetros

- `Length` – número de barras usadas para autocorrelação e regressão.
- `Threshold Gain` – aumento mínimo esperado do preço para entrar em uma operação.
- `Signal Threshold` – nível de autocorrelação necessário para armazenar uma previsão.
- `Candle Type` – tipo de velas para os cálculos.
