# Graph Style 4th Dimension RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina a variação de preço com os níveis do RSI.

Os testes indicam um retorno anual médio de cerca de 80%. Funciona bem em mercados voláteis.

A estratégia verifica a direção da última variação de preço juntamente com os extremos do RSI. Abre uma posição quando o RSI sai das zonas de sobrecompra/sobrevenda e a variação de preço recente confirma o movimento. As posições são fechadas quando o RSI retorna à área intermediária ou um sinal oposto aparece.

## Detalhes

- **Critérios de entrada**: Direção da variação de preço com extremo do RSI.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto ou RSI de volta ao meio.
- **Stops**: Stop loss percentual.
- **Valores padrão**:
  - `RsiPeriod` = 14
  - `OverboughtLevel` = 70m
  - `OversoldLevel` = 30m
  - `StopLossPercent` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: RSI
  - Stops: Percentual
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
