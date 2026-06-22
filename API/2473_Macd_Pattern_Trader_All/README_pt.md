# Estratégia de Padrão Trader MACD (All)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que abre posições em reversões bruscas do MACD. Ela procura dois grandes picos em torno de um pequeno valor intermediário da linha MACD. Uma venda é aberta quando o valor anterior do MACD é positivo e o valor atual cai profundamente em território negativo. Uma compra é aberta na condição oposta. O stop loss e o take profit são derivados das máximas e mínimas recentes.

O algoritmo se adapta a mercados voláteis onde o momentum muda de direção rapidamente. Utiliza apenas ordens a mercado e calcula os níveis de risco a partir do histórico de velas.

## Detalhes

- **Critérios de entrada**: Relação de picos MACD baseada em `RatioThreshold`.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Stop no extremo recente mais o offset ou pico oposto.
- **Stops**: Sim.
- **Valores padrão**:
  - `FastEmaPeriod` = 24
  - `SlowEmaPeriod` = 13
  - `StopLossBars` = 22
  - `TakeProfitBars` = 32
  - `OffsetPoints` = 40
  - `RatioThreshold` = 5m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: MACD
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
