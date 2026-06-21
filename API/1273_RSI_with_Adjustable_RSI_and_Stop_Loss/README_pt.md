# Estratégia RSI com RSI Ajustável e Stop-Loss
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Compra quando o valor do RSI cai abaixo de um limiar e fecha a posição comprada quando o preço rompe acima da máxima do candle anterior. Um stop loss percentual protege cada operação.

## Detalhes

- **Critérios de entrada**:
  - Comprado: RSI abaixo de `RsiThreshold`
- **Comprado/Vendido**: Comprado
- **Critérios de saída**:
  - Preço de fechamento acima da máxima do candle anterior
  - Stop loss
- **Stops**: Sim
- **Valores padrão**:
  - `RsiLength` = 8
  - `RsiThreshold` = 28m
  - `StopLossPercent` = 5m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Comprado
  - Indicadores: RSI
  - Stops: Sim
  - Complexidade: Básico
  - Período: Curto prazo
  - Sazonalidade: Nenhum
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
