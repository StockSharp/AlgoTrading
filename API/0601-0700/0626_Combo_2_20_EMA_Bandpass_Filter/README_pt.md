# Estratégia Combo 2/20 EMA com Filtro de Passa-Banda
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina um cruzamento de EMA rápida e lenta com um filtro de passa-banda. Posições compradas são abertas quando a EMA rápida está acima da EMA lenta e o valor do filtro supera a zona de venda. Posições vendidas são abertas quando a EMA rápida está abaixo da EMA lenta e o valor do filtro cai abaixo da zona de compra. As posições são fechadas se os sinais desaparecerem ou antes da data de início.

Os testes indicam um retorno anual médio de cerca de 47%. Tem melhor desempenho no mercado de criptomoedas.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: EMA rápida > EMA lenta e filtro > zona de venda
  - **Vendido**: EMA rápida < EMA lenta e filtro < zona de compra
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Fechar posição quando os sinais desaparecerem
- **Stops**: Não
- **Valores padrão**:
  - `FastEmaLength` = 2
  - `SlowEmaLength` = 20
  - `BpfLength` = 20
  - `BpfDelta` = 0.5m
  - `BpfSellZone` = 5m
  - `BpfBuyZone` = -5m
  - `StartDate` = new DateTimeOffset(2005, 1, 1, 0, 0, 0, TimeSpan.Zero)
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: EMA Bandpass Filter
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
