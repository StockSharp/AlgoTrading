# Estratégia de Rompimento de Faixa ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia entra em posições compradas quando o fechamento rompe acima do fechamento mais alto de um período de retrospectiva enquanto o ADX permanece abaixo de um limiar especificado, indicando um mercado calmo. A negociação é limitada a uma sessão definida e a um número máximo de operações por dia. Um stop-loss fixo em unidades de preço protege cada posição.

## Detalhes

- **Critérios de entrada**: `Close >= previous highest close` e `ADX < threshold` dentro da sessão
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**: Stop-loss ou fim da sessão
- **Stops**: Sim
- **Valores padrão**:
  - `AdxPeriod` = 14
  - `HighestPeriod` = 34
  - `AdxThreshold` = 17.5
  - `StopLoss` = 1000
  - `MaxTradesPerDay` = 3
  - `CandleType` = TimeSpan.FromMinutes(30)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Somente comprado
  - Indicadores: ADX
  - Stops: Sim
  - Complexidade: Iniciante
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
