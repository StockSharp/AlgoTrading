# CCI Slope Estratégia de Reversão à Média
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A estratégia CCI Slope de Reversão à Média concentra-se em leituras extremas do CCI para explorar a reversão. Desvios amplos do nível típico raramente persistem.

As operações são acionadas quando o indicador se afasta muito de sua média e então começa a reverter. Tanto as configurações compradas quanto as vendidas incluem um stop de proteção.

Adequada para traders de swing que esperam oscilações, a estratégia encerra a posição assim que o CCI retorna ao equilíbrio. Parâmetro inicial `CciPeriod` = 20.

## Detalhes

- **Critérios de entrada**: O indicador cruza de volta em direção à média.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: O indicador reverte para a média.
- **Stops**: Sim.
- **Valores padrão**:
  - `CciPeriod` = 20
  - `SlopeLookback` = 20
  - `ThresholdMultiplier` = 2m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: CCI
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
