# Estratégia Crypto SUSDT 10 min
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma simples estratégia de cruzamento de EMA que entra comprado quando o preço fecha acima da EMA e abre abaixo dela, e entra vendido na condição oposta. O stop loss e o take profit são definidos como percentuais do preço de entrada.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `close > EMA` e `open < EMA`
  - **Vendido**: `close < EMA` e `open > EMA`
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Take profit ou stop loss.
- **Stops**: Sim, tanto take profit quanto stop loss.
- **Valores padrão**:
  - `CandleType` = 10 minutos
  - `EmaLength` = 24
  - `TakeProfitPercent` = 4
  - `StopLossPercent` = 2
  - `OrderPercent` = 30
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: EMA
  - Stops: Sim
  - Complexidade: Baixo
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
