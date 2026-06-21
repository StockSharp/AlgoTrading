# Estratégia Momentum Alligator 4h Bitcoin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Momentum Alligator 4h Bitcoin combina o Awesome Oscillator com o Alligator de Bill Williams no período diário. Uma posição comprada é aberta quando o oscilador cruza acima da sua SMA de 5 períodos e o preço opera acima das três linhas diárias do Alligator. Um stop loss dinâmico utiliza o maior valor entre uma queda percentual desde a entrada e a linha da mandíbula do Alligator. Após uma saída lucrativa, a estratégia ignora os próximos dois sinais.

## Detalhes

- **Critérios de entrada**: AO cruza acima da sua SMA de 5 períodos e o fechamento está acima das linhas diárias do Alligator.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Stop loss dinâmico no máximo entre o stop percentual e a mandíbula do Alligator.
- **Stops**: Sim.
- **Valores padrão**:
  - `StopLossPercent` = 0.02m
  - `CandleType` = TimeSpan.FromHours(4)
  - `TradeStart` = 2023-01-01
  - `TradeStop` = 2025-01-01
- **Filtros**:
  - Categoria: Momentum
  - Direção: Somente comprado
  - Indicadores: Awesome Oscillator, Alligator
  - Stops: Sim
  - Complexidade: Básico
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
