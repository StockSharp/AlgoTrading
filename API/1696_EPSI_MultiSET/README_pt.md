# Estratégia EPSI Multi SET
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de rompimento convertida do expert MQL4 original *e-PSI@MultiSET*.
Observa cada vela e entra quando o preço se move uma distância especificada a partir da abertura.
As posições utilizam níveis de take-profit e stop-loss e as operações são permitidas apenas durante
uma janela de tempo definida pelo usuário.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `High - Open >= MinDistance`
  - Vendido: `Open - Low >= MinDistance`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: TakeProfit ou StopLoss
- **Stops**: Sim
- **Valores padrão**:
  - `MinDistance` = 20
  - `TakeProfit` = 20
  - `StopLoss` = 200
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
  - `OpenHour` = 2
  - `CloseHour` = 20
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
