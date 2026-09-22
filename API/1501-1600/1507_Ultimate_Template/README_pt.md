# Modelo de Estratégia Ultimate
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de momentum do RSI com duas EMAs como filtro de tendência. Os sinais são avaliados em candles concluídos, enquanto o take profit e o stop loss percentuais permanecem ativos durante a pausa de 80 candles.

## Detalhes

- **Critérios de entrada**: Compra quando o RSI cruza 50 para cima e a EMA rápida está acima da lenta; venda quando cruza 50 para baixo e a EMA rápida está abaixo da lenta.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Cruzamento oposto do RSI, take profit ou stop loss.
- **Stops**: Stop loss e take profit em percentual.
- **Pausa**: 80 candles concluídos após uma entrada por sinal ou saída pelo RSI; não desativa as proteções de take profit e stop loss.
- **Valores padrão**:
  - `FastLength` = 9
  - `SlowLength` = 21
  - `StopLossPercent` = 1
  - `TakeProfitPercent` = 3
  - `CandleType` = 5 minutos
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: RSI, EMA
  - Stops: Sim
  - Complexidade: Básico
  - Período: Médio
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
