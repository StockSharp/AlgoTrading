# Estratégia de Cruzamento Ultra WPR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia aplica um oscilador Williams %R suavizado por duas médias móveis. O cruzamento das linhas rápida e lenta suavizadas gera sinais de trading. Uma posição comprada é aberta quando a linha rápida sobe acima da linha lenta, e uma posição vendida é aberta quando a linha rápida cai abaixo da linha lenta.

A abordagem busca seguir o momentum emergente enquanto limita o risco com níveis de take-profit e stop-loss configuráveis.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: Linha rápida cruza acima da linha lenta
  - **Vendido**: Linha rápida cruza abaixo da linha lenta
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - **Comprado**: Saída quando linha rápida cruza abaixo da linha lenta
  - **Vendido**: Saída quando linha rápida cruza acima da linha lenta
- **Stops**: Sim, take-profit e stop-loss baseados em preço
- **Valores padrão**:
  - `CandleType` = TimeSpan.FromHours(4)
  - `WprPeriod` = 13
  - `FastLength` = 3
  - `SlowLength` = 53
  - `TakeProfit` = 0.2m
  - `StopLoss` = 0.1m
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Williams %R, Moving Average
  - Stops: Sim
  - Complexidade: Básico
  - Período: H4
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
