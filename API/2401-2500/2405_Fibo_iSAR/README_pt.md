# Estratégia Fibo iSAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina indicadores Parabolic SAR rápido e lento com níveis de retração de Fibonacci. Quando o SAR rápido está acima do SAR lento e abaixo do preço, a estratégia antecipa uma tendência de alta e coloca uma ordem Buy Limit na retração de 50% de Fibonacci do intervalo recente. O stop loss é colocado abaixo da mínima do swing e o take profit na extensão de 161%. Para uma tendência de baixa, a lógica é espelhada com ordens Sell Limit.

## Detalhes

- **Critérios de entrada**: Direção de tendência pelo SAR rápido/lento; entrada na retração de Fibonacci de 50%.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Níveis de stop loss ou take profit.
- **Stops**: Sim.
- **Valores padrão**:
  - `StepFast` = 0.02
  - `MaximumFast` = 0.2
  - `StepSlow` = 0.01
  - `MaximumSlow` = 0.1
  - `CountBarSearch` = 3
  - `IndentStopLoss` = 30
  - `FiboEntranceLevel` = 50
  - `FiboProfitLevel` = 161
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Parabolic SAR, Fibonacci
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
