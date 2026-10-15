# Estratégia de Reversão à Média por Largura de Donchian
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Reversão à Média por Largura de Donchian concentra-se em leituras extremas do canal Donchian para explorar a reversão. Grandes desvios do nível normal raramente persistem.

Os testes indicam um retorno anual médio de aproximadamente 121%. A estratégia funciona melhor no mercado de criptomoedas.

As operações são acionadas quando o indicador se afasta muito da sua média e começa a reverter. Tanto as configurações compradas quanto as vendidas incluem um stop protetor.

Adequada para traders de swing que esperam oscilações, a estratégia fecha as posições assim que o canal Donchian retorna ao equilíbrio. Parâmetro inicial `DonchianPeriod` = 20.

## Detalhes

- **Critérios de entrada**: O indicador cruza de volta em direção à média.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: O indicador reverte para a média.
- **Stops**: Sim.
- **Valores padrão**:
  - `DonchianPeriod` = 20
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2.0m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Mean Reversion
  - Direção: Ambos
  - Indicadores: Donchian
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
