# Estratégia de Trading com Doji
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia procura velas Doji que apareçam acima de uma média móvel exponencial. Quando esse padrão ocorre, ela entra em uma posição comprada. O stop-loss é definido na mínima mais baixa das barras recentes e um stop trailing protege o lucro após o preço se mover suficientemente a favor.

## Detalhes

- **Critérios de entrada**: Vela Doji com fechamento acima da EMA.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Stop na mínima mais baixa e stop trailing.
- **Stops**: Sim, fixo e trailing.
- **Valores padrão**:
  - `CandleType` = 5 minutos
  - `EmaLength` = 60
  - `Tolerance` = 0.05
  - `StopBars` = 450
  - `TrailTriggerPercent` = 1
  - `TrailOffsetPercent` = 0.5
- **Filtros**:
  - Categoria: Padrão
  - Direção: Comprado
  - Indicadores: EMA, Candlestick
  - Stops: Sim
  - Complexidade: Iniciante
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
