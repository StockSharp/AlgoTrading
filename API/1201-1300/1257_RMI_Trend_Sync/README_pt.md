# Estratégia RMI Trend Sync
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

RMI Trend Sync combina sinais de momentum de RSI e MFI com um stop trailing de SuperTrend. Uma operação comprada abre quando o momentum médio cruza acima de um limiar com inclinação ascendente da EMA, enquanto uma operação vendida é acionada em uma ruptura descendente. O SuperTrend fornece o trail de saída.

## Detalhes

- **Critérios de entrada**: Média de momentum cruza limiares com confirmação da inclinação da EMA.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Momentum oposto ou stop de SuperTrend.
- **Stops**: Sim.
- **Valores padrão**:
  - `RmiLength` = 21
  - `PositiveThreshold` = 70
  - `NegativeThreshold` = 30
  - `SuperTrendLength` = 10
  - `SuperTrendMultiplier` = 3.5
  - `Direction` = TradeDirection.Both
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: RSI, MFI, EMA, SuperTrend
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
