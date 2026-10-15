# Estratégia BTCUSD com SLTP Ajustável
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia opera BTCUSD usando um cruzamento entre SMA(10) e SMA(25) com um filtro EMA(150). As entradas compradas aguardam um recuo: após o cruzamento, um percentual de retração é rastreado e uma posição comprada é aberta quando o preço cruza de volta acima desse nível. As entradas vendidas são acionadas imediatamente em um cruzamento de baixa enquanto o preço está abaixo da EMA.

As saídas usam distâncias ajustáveis de take-profit, stop-loss e break-even. Uma posição comprada também é encerrada se SMA(10) cruzar abaixo de SMA(25) enquanto o preço estiver abaixo de EMA(150).

## Detalhes

- **Critérios de entrada**:
  - Comprado: SMA(10) cruza acima de SMA(25), então o preço retrai um percentual definido e cruza acima do nível de retração.
  - Vendido: SMA(10) cruza abaixo de SMA(25) enquanto o preço está abaixo de EMA(150).
- **Comprado/Vendido**: Comprado e vendido.
- **Critérios de saída**:
  - Distâncias configuráveis de take-profit, stop-loss e break-even.
  - Saída comprada quando SMA(10) cruza abaixo de SMA(25) abaixo de EMA(150).
- **Stops**: Sim, ajustáveis em pontos.
- **Valores padrão**:
  - `FastSmaLength` = 10
  - `SlowSmaLength` = 25
  - `EmaFilterLength` = 150
  - `TakeProfitDistance` = 1000
  - `StopLossDistance` = 250
  - `BreakEvenTrigger` = 500
  - `RetracementPercentage` = 0.01
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado e Vendido
  - Indicadores: SMA, EMA
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
