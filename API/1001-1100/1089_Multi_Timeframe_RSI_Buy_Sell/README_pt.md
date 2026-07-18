# Estratégia de Compra/Venda RSI Multi-Período
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza valores RSI de três períodos de tempo diferentes. Uma posição comprada é aberta quando todos os RSI habilitados estão abaixo do limiar de compra. Uma posição vendida é aberta quando todos os RSI habilitados estão acima do limiar de venda. Um período de resfriamento evita sinais consecutivos.

## Detalhes

- **Critérios de entrada**: Todos os RSI ativos abaixo/acima dos limiares.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `Rsi1Length` = 14
  - `Rsi2Length` = 14
  - `Rsi3Length` = 14
  - `Rsi1CandleType` = TimeSpan.FromMinutes(5)
  - `Rsi2CandleType` = TimeSpan.FromMinutes(15)
  - `Rsi3CandleType` = TimeSpan.FromMinutes(30)
  - `BuyThreshold` = 30m
  - `SellThreshold` = 70m
  - `CooldownPeriod` = 5
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: RSI
  - Stops: Não
  - Complexidade: Básico
  - Período: Multi-período
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
