# Fib Hurst Rompimento
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Fib Hurst Rompimento combina níveis de retração de Fibonacci do período diário com um filtro de expoente de Hurst. O preço cruzando os níveis-chave de Fibonacci na direção da tendência prevalente dispara entradas, enquanto um stop de 2% e uma relação risco-recompensa de 1:2 gerenciam o risco.

## Detalhes

- **Critérios de entrada**:
  - Comprado: Fechamento cruza acima do nível de 61,8% e Hurst diário > 0,5
  - Vendido: Fechamento cruza abaixo do nível de 38,2% e Hurst diário < 0,5
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Stop-loss ou take-profit
- **Stops**: Sim
- **Valores padrão**:
  - `CandleType` = TimeSpan.FromMinutes(15)
  - `HurstPeriod` = 50
  - `MaxTradesPerDay` = 5
  - `MaxTotalTrades` = 510
  - `RiskPercent` = 2m
  - `RiskReward` = 2m
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Hurst, Fibonacci
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
