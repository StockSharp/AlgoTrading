# Rompimentos e Retestes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que entra em rompimentos de máximas ou mínimas recentes e em retestes opcionais com gerenciamento de stop trailing.

A abordagem rastreia suporte e resistência definidos pelos fechamentos mais altos e mais baixos ao longo de uma janela de retrospecto. Os rompimentos abrem posições na direção do rompimento ou aguardam um reteste do nível rompido. As saídas usam um stop-loss inicial que se transforma em stop trailing quando o lucro atinge um limiar.

## Detalhes

- **Critérios de entrada**: Rompimento acima da resistência ou abaixo do suporte, reteste opcional.
- **Comprado/Vendido**: Configurável.
- **Critérios de saída**: Stop trailing ou rompimento oposto.
- **Stops**: Stop-loss inicial e stop trailing.
- **Valores padrão**:
  - `LookbackPeriod` = 20
  - `RetestBarsSinceBreakout` = 2
  - `RetestDetectionLimit` = 2
  - `ProfitThresholdPercent` = 5m
  - `TrailingStopGapPercent` = 1m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Highest, Lowest
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
