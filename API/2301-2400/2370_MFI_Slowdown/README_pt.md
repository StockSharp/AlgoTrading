# Desaceleração MFI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia monitora o Índice de Fluxo de Dinheiro (MFI) em um período superior e reage quando atinge zonas extremas. Se `SeekSlowdown` estiver habilitado, um sinal é confirmado apenas quando o valor do MFI muda menos de um ponto entre duas barras consecutivas. Em um sinal ascendente, fecha posições vendidas e opcionalmente abre uma nova posição comprada; em um sinal descendente, fecha posições compradas e pode abrir uma vendida. O gerenciamento de risco é tratado pelo StartProtection.

## Detalhes

- **Critérios de entrada**:
  - Sinal ascendente: `MFI >= UpperThreshold` e (sem verificação de desaceleração ou desaceleração detectada).
  - Sinal descendente: `MFI <= LowerThreshold` e (sem verificação de desaceleração ou desaceleração detectada).
- **Comprado/Vendido**: Ambos, dependendo dos parâmetros.
- **Critérios de saída**:
  - O sinal oposto fecha a posição.
  - Stop-loss e take-profit via `StopLossPercent` e `TakeProfitPercent`.
- **Stops**: Sim, via StartProtection.
- **Valores padrão**:
  - `MfiPeriod` = 2
  - `UpperThreshold` = 90
  - `LowerThreshold` = 10
  - `SeekSlowdown` = true
  - `TakeProfitPercent` = 2
  - `StopLossPercent` = 1
  - `CandleType` = período de 6 horas
  - `BuyPosOpen` = `BuyPosClose` = `SellPosOpen` = `SellPosClose` = true
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: MFI
  - Stops: Sim
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Opcional (verificação de desaceleração)
  - Nível de risco: Médio
