# Estratégia de Reversão à Média por Inclinação do Hull MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Reversão à Média por Inclinação do Hull MA concentra-se em leituras extremas do indicador Hull MA para explorar a reversão. Grandes desvios do nível normal raramente persistem.

As operações são acionadas quando o indicador se afasta muito da sua média e começa a reverter. Tanto as configurações compradas quanto as vendidas incluem um stop protetor.

Adequada para traders de swing que esperam oscilações, a estratégia fecha as posições assim que o Hull MA retorna ao equilíbrio. Parâmetro inicial `HullPeriod` = 9.

## Detalhes

- **Critérios de entrada**: O indicador cruza de volta em direção à média.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: O indicador reverte para a média.
- **Stops**: Sim.
- **Valores padrão**:
  - `HullPeriod` = 9
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2.0m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Mean Reversion
  - Direção: Ambos
  - Indicadores: Hull MA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
