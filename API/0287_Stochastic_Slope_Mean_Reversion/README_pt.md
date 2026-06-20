# Stochastic Slope Estratégia de Reversão à Média
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A estratégia Stochastic Slope de Reversão à Média concentra-se em leituras extremas do Stochastic para explorar a reversão. Desvios amplos do nível normal raramente persistem.

As operações são acionadas quando o indicador se afasta muito de sua média e então começa a reverter. Tanto as configurações compradas quanto as vendidas incluem um stop de proteção.

Adequada para traders de swing que esperam oscilações, a estratégia encerra a posição assim que o Stochastic retorna ao equilíbrio. Parâmetro inicial `StochPeriod` = 14.

## Detalhes

- **Critérios de entrada**: O indicador cruza de volta em direção à média.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: O indicador reverte para a média.
- **Stops**: Sim.
- **Valores padrão**:
  - `StochPeriod` = 14
  - `StochKPeriod` = 3
  - `StochDPeriod` = 3
  - `SlopeLookback` = 20
  - `ThresholdMultiplier` = 2m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Stochastic
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
