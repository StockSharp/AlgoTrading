# Estratégia de Painel de Trading Multicurrency
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia emula o comportamento do consultor especialista MQL original "Multicurrency trading panel". Ela monitora três pares de divisas (EURUSD, USDJPY, GBPUSD) e compara a última vela com a anterior usando sete métricas simples (abertura, máximo, mínimo, (máximo+mínimo)/2, fechamento, (máximo+mínimo+fechamento)/3, (máximo+mínimo+fechamento+fechamento)/4).
Para cada comparação, uma pontuação de COMPRA ou VENDA é incrementada. Quando a negociação automática está ativada, a estratégia abre ou reverte posições num par se a pontuação de COMPRA superar a de VENDA ou vice-versa.

## Parâmetros
- **EURUSD** – primeiro instrumento.
- **USDJPY** – segundo instrumento.
- **GBPUSD** – terceiro instrumento.
- **Candle Type** – período das velas.
- **Auto Trade** – ativar/desativar a colocação automática de ordens.

A estratégia é uma demonstração simplificada e não se destina ao trading real.
