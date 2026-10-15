# Estratégia dupla AG MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma versão StockSharp do MetaTrader 4 especialista **AG.mq4**. O robô opera com dois cálculos de média móvel, convergência e divergência (MACD) que usam conjuntos de parâmetros diferentes. O MACD primário produz gatilhos de entrada, enquanto o MACD secundário (escalado) atua como um filtro direcional para evitar negociações contra-tendência e controlar saídas. A lógica reflete o especialista MQL4 original, avaliando apenas velas fechadas e reutilizando as verificações de sinal da linha de sinal que bloquearam os pedidos originais.

## Lógica de negociação
- **Indicadores**
  - Primário MACD: EMA rápida = `FastEmaLength`, EMA lenta = `SlowEmaLength`, sinal SMA = `SignalSmaLength`.
  - Secundário MACD: EMA rápida = `SlowEmaLength * 2`, EMA lenta = `FastEmaLength * 2`, sinal SMA = `SignalSmaLength * 2`.
- **Entrada longa**
  - A linha principal MACD primária está acima de sua linha de sinal.
  - A linha de sinal primária MACD é negativa (abaixo da linha d'água).
  - A linha principal secundária MACD está acima de sua linha de sinal.
  - A linha de sinal secundária MACD é negativa.
- **Entrada curta**
  - A linha principal MACD primária está abaixo de sua linha de sinal.
  - A linha de sinal primária MACD é positiva.
  - A linha principal secundária MACD está abaixo de sua linha de sinal.
  - A linha de sinal secundária MACD é positiva.
- **Regras de saída**
  - Feche as posições longas quando o secundário MACD ficar em baixa enquanto a linha de sinal primária permanecer acima de zero.
  - Feche as posições vendidas quando o secundário MACD se tornar otimista enquanto a linha de sinal primária permanecer abaixo de zero.
- A estratégia reage apenas às velas acabadas e ignora as barras inacabadas para evitar a repintura.

## Gerenciamento de posição
- Todas as ordens são ordens de mercado com volume fixo definido por `OrderVolume`.
- `MaxOpenOrders` espelha a entrada original `ORDER` e limita o número total de pedidos ativos mais posições abertas. Defina-o como `0` para remover a tampa.
- `StartProtection()` é ativado assim que a estratégia é iniciada para que o gerente de risco StockSharp possa monitorar a exposição aberta.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `OrderVolume` | Tamanho base do lote para novas negociações. |
| `FastEmaLength` | Período EMA rápido do MACD primário. |
| `SlowEmaLength` | Período EMA lento do MACD primário. |
| `SignalSmaLength` | Período de suavização de sinal para ambos os MACDs. |
| `MaxOpenOrders` | Número máximo de ordens ativas e posições abertas combinadas. Defina `0` como ilimitado. |
| `CandleType` | Período usado para construir velas para ambos os indicadores. |

## Notas
- O MACD secundário mantém a mesma ordem rápido/lento do EA original, mesmo que o período rápido se torne maior que o lento, para preservar os cálculos do autor.
- A estratégia não coloca ordens pendentes; ele abre ou fecha no mercado assim que as condições aparecem.
- Nenhum nível adicional de stop-loss ou take-profit é adicionado porque o especialista original confiou exclusivamente em reversões de sinal.
