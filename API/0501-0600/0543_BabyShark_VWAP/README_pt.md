# Estratégia BabyShark VWAP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina uma banda de preço médio ponderado por volume (VWAP) com um filtro RSI baseado em OBV. Operações compradas ocorrem quando o preço cai abaixo da banda de desvio inferior e o RSI sinaliza sobrevendido. Operações vendidas são acionadas quando o preço sobe acima da banda superior e o RSI está sobrecomprado.

Os stops utilizam uma pequena porcentagem de perda e as posições aguardam um período de resfriamento antes de reentrar.

## Detalhes

- **Critérios de entrada**: O preço cruza as bandas de desvio com confirmação do RSI.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Retorno ao VWAP ou stop-loss.
- **Stops**: Sim.
- **Valores padrão**:
  - `Length` = 60
  - `RsiLength` = 5
  - `HigherLevel` = 70
  - `LowerLevel` = 30
  - `Cooldown` = 10
  - `StopLossPercent` = 0.6m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: VWAP, RSI, OBV
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
