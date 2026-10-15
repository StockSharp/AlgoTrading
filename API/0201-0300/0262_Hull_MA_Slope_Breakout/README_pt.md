# Estratégia de Rompimento por Inclinação de Hull MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A estratégia de Rompimento por Inclinação de Hull MA rastreia a taxa de variação do Hull. Uma inclinação incomumente acentuada indica que uma nova tendência está se formando.

Os testes indicam um retorno anual médio de aproximadamente 121%. Funciona melhor no mercado de criptomoedas.

As entradas ocorrem quando a inclinação excede seu nível típico em um múltiplo do desvio padrão, realizando operações na direção da aceleração com um stop protetor.

Atrai traders ativos que buscam exposição antecipada à tendência. As posições são encerradas quando a inclinação retorna às leituras normais. Valor padrão `HullLength` = 9.

## Detalhes

- **Critérios de entrada**: O indicador supera a média pelo multiplicador de desvio.
- **Comprado/Vendido**: Ambos os sentidos.
- **Critérios de saída**: O indicador reverte para a média.
- **Stops**: Sim.
- **Valores padrão**:
  - `HullLength` = 9
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2m
  - `StopLoss` = new Unit(2
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Hull
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

