# Estratégia de Cruzamento EMA 2-35
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia segue um simples cruzamento entre duas Médias Móveis Exponenciais. A EMA rápida com comprimento 2 reage rapidamente às mudanças de preço, enquanto a EMA lenta com comprimento 35 representa a tendência de longo prazo. Uma posição é aberta quando a EMA rápida cruza a EMA lenta; as posições são invertidas quando ocorre o cruzamento oposto.

O gerenciamento de risco é tratado com níveis fixos de stop-loss e take-profit expressos em passos de preço. Um trailing stop também é aplicado para garantir lucros à medida que a negociação avança em uma direção favorável.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: EMA(2) cruza acima de EMA(35).
  - **Vendido**: EMA(2) cruza abaixo de EMA(35).
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Cruzamento oposto.
  - Stop-loss ou take-profit atingido.
  - Trailing stop acionado.
- **Stops**: Stop-loss fixo, take-profit e trailing stop (todos em passos de preço).
- **Valores padrão**:
  - `FastLength` = 2
  - `SlowLength` = 35
  - `StopLoss` = 50
  - `TakeProfit` = 150
  - `TrailingStop` = 50
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Médias móveis
  - Stops: Sim
  - Complexidade: Simples
  - Período: Curto prazo

