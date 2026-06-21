# Rompimento Hardcore FX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Adaptação do especialista MetaTrader "HardcoreFX". A estratégia rastreia máximas e mínimas dos pivôs do ZigZag e abre posições quando o preço as rompe. Aplica níveis fixos de stop loss e take profit e também usa trailing stop para proteger os ganhos acumulados.

## Detalhes
- **Critérios de entrada**: Fechamento acima da última máxima do ZigZag para comprar; fechamento abaixo da última mínima do ZigZag para vender.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Acionamento de stop loss, take profit ou stop trailing.
- **Stops**: Stop loss fixo, take profit e stop trailing.
- **Valores padrão**:
  - `ZigzagLength` = 17
  - `StopLoss` = 1400
  - `TakeProfit` = 5400
  - `TrailingStop` = 500
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Highest, Lowest
  - Stops: Stop loss, Take profit, Stop trailing
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
