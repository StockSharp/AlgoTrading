# Estratégia de Compra e Venda com Padrão Envolvente de Alta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia entra comprado quando uma vela de alta envolve completamente a barra de baixa anterior e condições de tendência opcionais são atendidas. O tamanho da posição é uma porcentagem do capital atual, enquanto o take profit e o stop loss fecham as operações automaticamente.

## Detalhes

- **Critérios de entrada**: Padrão envolvente de alta com filtro de tendência SMA opcional.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Take profit ou stop loss.
- **Stops**: Sim, tanto take profit quanto stop loss.
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `TakeProfitPercent` = 2
  - `StopLossPercent` = 2
  - `OrderPercent` = 30
  - `TrendMode` = SMA50
- **Filtros**:
  - Categoria: Padrão
  - Direção: Somente comprado
  - Indicadores: Candlestick, SMA
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
