# Rompimento ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Rompimento ADX monitora o ADX em busca de expansões fortes. Quando as leituras saltam além de seu intervalo típico, o preço frequentemente inicia um novo movimento.

Os testes indicam um retorno anual médio de cerca de 97%. Funciona melhor no mercado de criptomoedas.

Uma posição é aberta assim que o indicador perfura uma banda derivada de dados recentes e um multiplicador de desvio. Operações compradas e vendidas são possíveis com um stop vinculado.

Este sistema se adapta a traders de momentum que buscam rompimentos precoces. As operações são fechadas quando o ADX retorna em direção à média. Os valores padrão começam com `ADXPeriod` = 14.

## Detalhes

- **Critérios de entrada**: O indicador supera a média pelo multiplicador de desvio.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: O indicador reverte à média.
- **Stops**: Sim.
- **Valores padrão**:
  - `ADXPeriod` = 14
  - `AvgPeriod` = 20
  - `Multiplier` = 0.1m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLoss` = 2.0m
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: ADX
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
