# Estratégia de Rompimento por Largura de Canal Keltner
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A estratégia de Rompimento por Largura de Canal Keltner observa o Keltner em busca de expansões rápidas. Quando as leituras saltam além do seu intervalo típico, o preço frequentemente inicia um novo movimento.

Os testes indicam um retorno anual médio de aproximadamente 112%. Funciona melhor no mercado forex.

Uma posição é aberta assim que o indicador perfura uma banda derivada de dados recentes e um multiplicador de desvio. Operações compradas e vendidas são possíveis com um stop associado.

Este sistema é adequado para traders de momentum que buscam rompimentos antecipados. As operações são encerradas quando o Keltner volta em direção à média. Os valores padrão começam com `EMAPeriod` = 20.

## Detalhes

- **Critérios de entrada**: O indicador supera a média pelo multiplicador de desvio.
- **Comprado/Vendido**: Ambos os sentidos.
- **Critérios de saída**: O indicador reverte para a média.
- **Stops**: Sim.
- **Valores padrão**:
  - `EMAPeriod` = 20
  - `ATRPeriod` = 14
  - `ATRMultiplier` = 2.0m
  - `AvgPeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopMultiplier` = 2
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Keltner
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

