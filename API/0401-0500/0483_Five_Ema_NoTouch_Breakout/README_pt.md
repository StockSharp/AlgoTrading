# Estratégia de Rompimento Sem Toque de 5 EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Rompimento Sem Toque de 5 EMA aguarda um candle que permaneça completamente de um lado da EMA de 5 períodos. Quando o preço posteriormente rompe o extremo desse candle de configuração, a estratégia entra na direção do rompimento. O stop-loss é colocado no extremo oposto e o take-profit é definido em um múltiplo do risco.

## Detalhes

- **Critérios de entrada**:
  - Máxima do candle abaixo da EMA → preparar comprado; entrar quando o preço romper acima da máxima desse candle.
  - Mínima do candle acima da EMA → preparar vendido; entrar quando o preço romper abaixo da mínima desse candle.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Stop no extremo do candle de configuração.
  - Alvo em `RewardRisk` × risco.
- **Stops**: Sim.
- **Valores padrão**:
  - `EmaPeriod` = 5
  - `RewardRisk` = 3.0
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Comprado/Vendido
  - Indicadores: EMA
  - Stops: Sim
  - Complexidade: Baixo
  - Período: 5 minutos
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
