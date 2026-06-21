# Estratégia de Cruzamento de Médias Móveis Swing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Opera quando uma média móvel exponencial rápida cruza uma média intermediária, com confirmação opcional de uma MA lenta e o histograma MACD. Utiliza stop loss e take profit baseados em ATR e pode sair em um cruzamento secundário de MA.

## Detalhes

- **Critérios de entrada**:
  - EMA rápida cruza acima da EMA média para comprado, abaixo para vendido.
  - Opcional: preço acima/abaixo da EMA lenta.
  - Opcional: histograma MACD acima/abaixo de zero.
- **Comprado/Vendido**: Configurável.
- **Critérios de saída**: Stop loss e take profit baseados em ATR ou cruzamento de MA de saída opcional.
- **Stops**: Sim, múltiplos de ATR.
- **Valores padrão**:
  - `FastPeriod` = 5
  - `MediumPeriod` = 10
  - `SlowPeriod` = 50
  - `FastExitPeriod` = 5
  - `MediumExitPeriod` = 10
  - `AtrPeriod` = 14
  - `AtrStopMultiplier` = 1.4
  - `AtrTakeMultiplier` = 3.2
  - `EnableSlow` = true
  - `EnableMacd` = true
  - `EnableLong` = true
  - `EnableShort` = false
  - `EnableCrossExit` = true
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Configurável
  - Indicadores: EMA, MACD, ATR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: 1m (padrão)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
