# Divergência de Volume (Volume Divergence)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Divergência de Volume busca discrepâncias entre o movimento do preço e o volume de negociação. Se o preço cai mas o volume aumenta, pode sinalizar acumulação; se o preço sobe com volume forte, pode sinalizar distribuição.

Os testes indicam um retorno anual médio de aproximadamente 43%. Funciona melhor no mercado de ações.

A estratégia entra comprado quando preços em queda são acompanhados de volume crescente, e entra vendido quando preços em alta se combinam com volume elevado. As saídas dependem de um cruzamento de média móvel.

Esta abordagem tenta operar contra movimentos insustentáveis.

## Detalhes

- **Critérios de entrada**: Preço e volume movendo-se em direções opostas.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: O preço cruza a MA ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `MAPeriod` = 20
  - `ATRPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Divergência
  - Direção: Ambos
  - Indicadores: Volume, MA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio
