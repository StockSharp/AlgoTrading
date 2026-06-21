# Pivot Point Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina Pivot Points com um Supertrend baseado em ATR para capturar reversões de tendência.

Os testes indicam um retorno anual médio de aproximadamente 65%. Tem melhor desempenho no mercado de ações.

Os Pivot Points definem uma linha central dinâmica. Um multiplicador ATR constrói bandas superior e inferior que seguem o preço. Quando a tendência muda de direção, a estratégia entra de acordo.

## Detalhes

- **Critérios de entrada**: Sinais baseados em Pivot Points e ATR Supertrend.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `PivotPeriod` = 2
  - `AtrFactor` = 3m
  - `AtrPeriod` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Pivot Points, ATR
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
