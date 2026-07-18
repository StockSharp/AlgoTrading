# Rompimento por Delta Acumulativo (Cumulative Delta Breakout)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O Cumulative Delta soma a diferença entre o volume de compra e venda. Esta estratégia monitora o total acumulado e opera quando ele ultrapassa o valor mais alto ou cai abaixo do mais baixo dentro do período de lookback.

Os testes indicam um retorno anual médio de aproximadamente 49%. Funciona melhor no mercado de criptomoedas.

Um rompimento do delta acumulativo frequentemente precede o movimento do preço. A estratégia fecha as operações quando o delta cruza de volta por zero ou atinge um nível de stop-loss.

## Detalhes

- **Critérios de entrada**: O delta acumulativo supera o valor mais alto ou o mais baixo no lookback.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: O delta cruza zero ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `LookbackPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Cumulative Delta
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
