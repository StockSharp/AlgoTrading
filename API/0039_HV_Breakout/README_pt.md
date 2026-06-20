# Historical Volatility Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Este método de rompimento usa a volatilidade histórica para definir limiares dinâmicos. Quando o preço se move além de um nível de referência por mais do que a volatilidade atual, indica uma tendência potencial.

Os testes indicam um retorno anual médio de aproximadamente 154%. Funciona melhor no mercado de ações.

A estratégia compara o preço com níveis derivados do desvio padrão e uma média móvel simples. Rompimentos acima ou abaixo desses níveis acionam operações.

As saídas ocorrem quando o preço cruza de volta pela média móvel ou o stop é atingido.

## Detalhes

- **Critérios de entrada**: Preço rompe acima ou abaixo do nível baseado em HV.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Preço cruza MA ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `HvPeriod` = 20
  - `MAPeriod` = 20
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: HV, MA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

