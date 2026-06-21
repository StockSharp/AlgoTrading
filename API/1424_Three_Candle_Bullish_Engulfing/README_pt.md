# Estratégia de Três Velas de Engolfamento Altista
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia busca um padrão de engolfamento altista ou baixista de três velas. Suporta entradas opcionais por rompimento de RSI, um stop de trailing e saídas baseadas em tempo.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Vela altista, pequeno doji e vela de engolfamento altista.
  - **Vendido**: Vela baixista, pequeno doji e vela de engolfamento baixista.
- **Comprado/Vendido**: Ambos (modo somente comprado disponível).
- **Critérios de saída**:
  - Stop de trailing, rompimento de vela oposta ou fim de sessão.
- **Stops**: Sim.
- **Valores padrão**:
  - `TrailPerc` = 1.5
  - `ExitHour` = 15
  - `ExitMinute` = 15
  - `RsiLength` = 14
  - `RsiLevel` = 80
  - `StopLossPerc` = 5
