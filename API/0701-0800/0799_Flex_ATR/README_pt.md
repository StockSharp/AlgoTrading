# Estratégia Flex ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Flex ATR seleciona dinamicamente os períodos de EMA, RSI e ATR com base no período atual. Uma operação comprada é aberta quando a EMA rápida cruza acima da lenta e o RSI supera 50. Uma operação vendida é ativada no cruzamento inverso com RSI abaixo de 50. As saídas usam stops baseados em ATR ou um trailing stop opcional.

## Detalhes

- **Critérios de entrada**: Cruzamento de EMA rápida vs lenta com filtro RSI.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Stop ou alvo baseado em ATR, trailing stop opcional.
- **Stops**: Sim.
- **Valores padrão**:
  - `AtrStopMult` = 3
  - `AtrProfitMult` = 1.5
  - `EnableTrailingStop` = true
  - `AtrTrailMult` = 1
  - `CandleType` = TimeSpan.FromMinutes(30)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: EMA, RSI, ATR
  - Stops: Sim
  - Complexidade: Avançado
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
