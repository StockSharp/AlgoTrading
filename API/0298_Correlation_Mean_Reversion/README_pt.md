# Estratégia de Reversão à Média por Correlação
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Reversão à Média por Correlação foca em leituras extremas da Correlação para explorar a reversão. Grandes desvios em relação ao nível típico raramente persistem.

As operações são acionadas quando o indicador oscila muito além da sua média e então começa a reverter. Tanto as configurações compradas quanto as vendidas incluem um stop de proteção.

Adequada para traders de swing que esperam oscilações, a estratégia fecha a posição assim que a Correlação retorna ao equilíbrio. Parâmetro inicial `CorrelationPeriod` = 20.

## Detalhes

- **Critérios de entrada**: O indicador cruza de volta em direção à média.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: O indicador reverte para a média.
- **Stops**: Sim.
- **Valores padrão**:
  - `CorrelationPeriod` = 20
  - `LookbackPeriod` = 20
  - `DeviationThreshold` = 2.0m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Correlation
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
