# Estratégia SupportResistTrade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de rompimento portada do MetaTrader que combina um filtro de tendência EMA de longo prazo com níveis dinâmicos de suporte e resistência. Observa o intervalo de oscilação recente, aguarda que o preço rompa o teto ou piso anterior na direção da tendência, e gerencia posições com trailing stops escalonados em pips.

## Detalhes

- **Critérios de entrada**: o preço fecha além do máximo (comprado) ou mínimo (vendido) do período `Lookback` anterior e a barra abre acima/abaixo da EMA `MaPeriod`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: o trailing stop é acionado ou uma posição lucrativa cruza de volta pela banda de suporte/resistência atualizada
- **Stops**: stop inicial na banda oposta, trail após movimentos de +20/+40/+60 pips (assegurando 10/20/30 pips respectivamente)
- **Valores padrão**:
  - `Lookback` = 55
  - `MaPeriod` = 500
  - `CandleType` = 1 minuto
  - `OrderVolume` = 0.1
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: EMA, Highest, Lowest
  - Stops: Trailing
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
