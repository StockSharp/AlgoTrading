# Estratégia de Bot de Scalping com Rompimento de Sessão
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Session Breakout Scalper opera rompimentos do intervalo de preços formado durante uma sessão predefinida.

## Detalhes

- **Critérios de entrada**: o preço rompe acima da máxima da sessão ou abaixo da mínima da sessão
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: take profit ou stop loss
- **Stops**: ATR ou fixo
- **Valores padrão**:
  - `SessionStart` = 01:00
  - `SessionEnd` = 02:00
  - `TakeProfit` = 100
  - `StopLoss` = 50
  - `UseAtrStop` = true
  - `AtrLength` = 14
  - `AtrMultiplier` = 2
  - `CandleType` = time frame 1 minute
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: ATR
  - Stops: ATR
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
