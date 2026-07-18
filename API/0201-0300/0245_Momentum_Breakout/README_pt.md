# Estratégia de Rompimento por Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este sistema de rompimento busca surgimentos repentinos de momentum em relação à sua média histórica. Quando as leituras de momentum superam a média por uma grande margem, o preço pode estar iniciando um movimento rápido e direcional.

Os testes indicam um retorno anual médio de cerca de 82%. Funciona melhor no mercado de ações.

A estratégia compra quando o momentum sobe acima da média mais `Multiplier` vezes seu desvio padrão. Um vendido é iniciado quando o momentum cai abaixo da média menos o mesmo multiplicador. As posições são fechadas assim que o momentum retorna em direção à sua média.

Os traders que gostam de movimentos rápidos podem apreciar as regras claras para capturar explosões de força. Um stop-loss baseado em porcentagem do preço protege contra rompimentos fracassados.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: Momentum > Avg + Multiplier * StdDev
  - **Vendido**: Momentum < Avg - Multiplier * StdDev
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair quando Momentum < Avg
  - **Vendido**: Sair quando Momentum > Avg
- **Stops**: Sim, stop-loss percentual.
- **Valores padrão**:
  - `MomentumPeriod` = 14
  - `AveragePeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Momentum
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
