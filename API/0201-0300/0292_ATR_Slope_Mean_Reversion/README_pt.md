# ATR Slope Estratégia de Reversão à Média
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A estratégia ATR Slope de Reversão à Média concentra-se em leituras extremas do ATR para explorar a reversão. Desvios amplos do nível normal raramente persistem.

As operações são acionadas quando o indicador se afasta muito de sua média e então começa a reverter. Tanto as configurações compradas quanto as vendidas incluem um stop de proteção.

Adequada para traders de swing que esperam oscilações, a estratégia encerra a posição assim que o ATR retorna ao equilíbrio. Parâmetro inicial `AtrPeriod` = 14.

## Detalhes

- **Critérios de entrada**: O indicador cruza de volta em direção à média.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: O indicador reverte para a média.
- **Stops**: Sim.
- **Valores padrão**:
  - `AtrPeriod` = 14
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2.0m
  - `StopLossMultiplier` = 2
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: ATR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
