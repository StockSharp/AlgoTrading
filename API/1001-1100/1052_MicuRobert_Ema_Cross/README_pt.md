# Estratégia de Cruzamento EMA de MicuRobert
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza duas Médias Móveis Exponenciais de Lag Zero (ZLEMA) para operar cruzamentos. É possível restringir as operações a uma sessão específica e usar opcionalmente um trailing stop.

## Detalhes

- **Critérios de entrada:**
  - **Comprado:** ZLEMA rápida cruza acima da ZLEMA lenta, ou o preço cruza acima da ZLEMA rápida enquanto a rápida está acima da lenta.
  - **Vendido:** ZLEMA rápida cruza abaixo da ZLEMA lenta, ou o preço cruza abaixo da ZLEMA rápida enquanto a rápida está abaixo da lenta.
- **Critérios de saída:** posições fecham por trailing stop ou por níveis fixos de stop-loss e take-profit.
- **Stops:** trailing stop opcional com take-profit e stop-loss fixos.
- **Filtros:** filtro de horário de sessão.
