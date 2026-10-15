# Estratégia Turtle Trader V1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Turtle Trader V1 combina múltiplos osciladores de momentum com um filtro de média móvel. Uma posição comprada é aberta quando a EMA rápida está acima da EMA lenta e RSI, Stochastic, CCI, Momentum e o oscilador Chaikin apontam para cima. Posições vendidas requerem as condições opostas.

## Detalhes

- **Critérios de entrada**:
  - EMA rápida acima da EMA lenta (abaixo para vendidos)
  - RSI subindo e abaixo de 70 para comprados, RSI caindo e acima de 30 para vendidos
  - Stochastic %K abaixo de 88 para comprados, acima de 12 para vendidos
  - CCI e Momentum aumentando para comprados, diminuindo para vendidos
  - Oscilador Chaikin movendo-se na direção da operação
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: sinal oposto
- **Stops**: nenhum por padrão
- **Valores padrão**:
  - `FastMaPeriod` = 10
  - `SlowMaPeriod` = 50
  - `RsiPeriod` = 14
  - `StochPeriod` = 14
  - `CciPeriod` = 20
  - `MomentumPeriod` = 10
  - `ChoFastPeriod` = 3
  - `ChoSlowPeriod` = 10
- **Filtros**:
  - Categoria: Seguidor de tendência / Momentum
  - Direção: Ambos
  - Indicadores: EMA, RSI, Stochastic, CCI, Momentum, Chaikin Oscillator
  - Stops: Nenhum
  - Complexidade: Avançado
  - Período: 1 hora
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
