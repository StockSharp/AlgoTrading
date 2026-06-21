# Estratégia PSAR Trader Ticks
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no indicador Parabolic SAR. PSAR Trader Ticks segue os pontos do indicador Parabolic SAR e reage quando o preço cruza de um lado para o outro. Abre uma posição comprada quando o preço se move acima do SAR e uma posição vendida quando o preço se move abaixo dele. O trading pode ser restrito a um intervalo de tempo específico, e as posições existentes podem ser fechadas opcionalmente quando um sinal contrário aparece. A estratégia também aplica níveis de take-profit e stop-loss medidos em ticks.

## Detalhes

- **Critérios de entrada**: Preço cruzando o indicador Parabolic SAR.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal contrário (opcional), stop-loss ou take-profit.
- **Stops**: Take-profit e stop-loss em ticks.
- **Valores padrão**:
  - `Step` = 0.001m
  - `Maximum` = 0.2m
  - `TakeProfitTicks` = 50
  - `StopLossTicks` = 50
  - `StartHour` = 0
  - `EndHour` = 23
  - `CloseOnOpposite` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Parabolic SAR
  - Stops: Take-profit, Stop-loss
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
