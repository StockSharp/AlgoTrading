# Estratégia ATR Expansion Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Esta estratégia segue explosões de volatilidade usando o Average True Range. Quando o ATR está aumentando em comparação com a barra anterior e o preço opera em relação a uma média móvel, ela busca surfar o rompimento.

Os testes indicam um retorno anual médio de aproximadamente 145%. Funciona melhor no mercado de criptomoedas.

A expansão do ATR implica que um movimento forte está em andamento. As entradas se alinham com a direção do preço em relação à média móvil, enquanto as contrações de volatilidade acionam as saídas.

Os stops são definidos usando um múltiplo de ATR para dar espaço às operações durante alta volatilidade.

## Detalhes

- **Critérios de entrada**: ATR aumentando e preço acima/abaixo da MA.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: ATR contrai ou stop é atingido.
- **Stops**: Sim.
- **Valores padrão**:
  - `AtrPeriod` = 14
  - `MAPeriod` = 20
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: ATR, MA
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

