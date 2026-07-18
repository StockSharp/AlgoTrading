# Estratégia de Trading por Teoria dos Jogos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia de Trading por Teoria dos Jogos combina análise de comportamento de manada, detecção de armadilhas de liquidez, fluxo institucional e zonas de equilíbrio de Nash para operar movimentos contrários e de momentum.

A estratégia monitora extremos do RSI e picos de volume para identificar compras ou vendas em massa. Armadilhas de liquidez próximas a máximas e mínimas recentes, além do indicador de acumulação/distribuição e viés de dinheiro inteligente, refinam as entradas. Bandas de preço construídas a partir de uma média móvel e desvio padrão definem o equilíbrio de Nash para operações de reversão. O tamanho da posição se adapta quando o preço está perto do equilíbrio ou aparece volume institucional.

## Detalhes
- **Dados**: Velas de preço e volume.
- **Critérios de entrada**: Sinais contrários, de momentum ou de reversão Nash.
- **Critérios de saída**: Stop loss / take profit ou sinais opostos.
- **Stops**: Stop loss e take profit opcionais.
- **Valores padrão**:
  - `RsiLength` = 14
  - `VolumeMaLength` = 20
  - `HerdThreshold` = 2.0
  - `LiquidityLookback` = 50
  - `InstVolumeMultiplier` = 2.5
  - `InstMaLength` = 21
  - `NashPeriod` = 100
  - `NashDeviation` = 0.02
  - `UseStopLoss` = True
  - `StopLossPercent` = 2
  - `UseTakeProfit` = True
  - `TakeProfitPercent` = 5
- **Filtros**:
  - Categoria: Misto contrário/momentum
  - Direção: Comprado e Vendido
  - Indicadores: RSI, SMA, Accumulation/Distribution, StandardDeviation, Highest/Lowest
  - Complexidade: Avançado
  - Nível de risco: Médio
