# Sistema Turtle Trading
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Sistema clássico de Turtle Trading usando rompimentos de canais Donchian e gestão de risco baseada em ATR.

## Detalhes

- **Critérios de entrada**: rompimento da banda superior/inferior do canal Donchian
- **Comprado/Vendido**: ambos
- **Critérios de saída**: cruzamento do canal Donchian mais curto ou stop trailing
- **Stops**: stop inicial e trailing baseado em ATR
- **Valores padrão**:
  - `EntryLength` = 20
  - `ExitLength` = 10
  - `EntryLengthMode2` = 55
  - `ExitLengthMode2` = 20
  - `AtrPeriod` = 14
  - `RiskPerTrade` = 0.02
  - `InitialStopAtrMultiple` = 2
  - `PyramidAtrMultiple` = 0.5
  - `MaxUnits` = 4
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: DonchianChannels, ATR
  - Stops: ATR
  - Complexidade: Avançado
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
