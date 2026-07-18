# Estratégia Sidus EMA RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma versão StockSharp do consultor especialista MetaTrader 4 **Exp_Sidus.mq4**. Reproduz a lógica original de que
combina um cruzamento rápido/EMA lenta com um filtro RSI de 50 níveis. Os sinais são avaliados apenas em velas concluídas e cada vela pode
gerar no máximo um pedido, correspondendo à disciplina de tempo do robô de origem.

## Lógica de negociação

- **Pilha de indicadores**
  - Média móvel exponencial rápida (período padrão 5)
  - Média móvel exponencial lenta (período padrão 12)
  - Índice de Força Relativa (período padrão 21)
- **Configuração de alta**
  1. O EMA rápido estava abaixo ou igual ao EMA lento na vela de sinal anterior.
  2. O EMA rápido está acima do EMA lento na vela de sinal atual.
  3. RSI na mesma vela é estritamente maior que 50.
- **Configuração de baixa**
  1. O EMA rápido estava acima ou igual ao EMA lento na vela de sinal anterior.
  2. O EMA rápido está abaixo do EMA lento na vela de sinal atual.
  3. RSI na mesma vela é estritamente menor que 50.
- **Signal shift** — o parâmetro `SignalShift` (padrão `1`) define qual vela fechada é considerada a barra de sinal "atual".
Um valor de `1` usa a última vela fechada, `0` usa a vela recém-fechada, `2` olha duas barras atrás e assim por diante. O anterior
vela para detecção de cruzamento é calculada automaticamente como `SignalShift + 1`.
- **Proteção duplicada** — a estratégia armazena o tempo de abertura da vela sinalizadora e nunca abre outra posição vinculada ao
mesma barra, imitando fielmente a verificação `LastTime` no EA original.

## Gerenciamento de posição

- Existe apenas uma posição por vez.
- Quando um sinal oposto aparece enquanto uma posição está aberta, a estratégia primeiro fecha a posição existente e depois espera pela
próxima passagem de processamento para abrir uma negociação na nova direção, exatamente como a versão MQL faz.
- `StartProtection` anexa colchetes opcionais de take-profit e stop-loss expressos em faixas de preço (etapas de preço). As distâncias são
derivado das entradas do EA original: pontos de take-profit padrão `80` e pontos de stop-loss `20`.

## Parâmetros

| Nome | Descrição | Padrão | Notas |
| ---- | ----------- | ------- | ----- |
| `TakeProfitPoints` | Distância de lucro em etapas de preço. | `80` | Defina `0` para desativar o alvo. |
| `StopLossPoints` | Distância de stop-loss em etapas de preço. | `20` | Defina `0` para desativar a proteção. |
| `TradeVolume` | Volume de pedidos (lotes/contratos). | `0.1` | Atribuído à propriedade base `Volume` no início. |
| `FastPeriod` | Comprimento EMA rápido. | `5` | Otimizável. |
| `SlowPeriod` | Comprimento EMA lento. | `12` | Otimizável. |
| `RsiPeriod` | RSI comprimento. | `21` | Otimizável. |
| `SignalShift` | Número de velas fechadas usadas para cálculos de sinal. | `1` | Espelha a entrada `shif` do MT4 EA. |
| `CandleType` | Fonte de vela para a assinatura. | `1h` período de tempo | Pode ser definido como qualquer `DataType` compatível com o ambiente. |

## Notas de implementação

- Os dados da vela são inscritos via `SubscribeCandles(CandleType)` e processados dentro de `ProcessCandle` somente depois que a vela atinge
o estado `Finished`.
- Os valores dos indicadores são armazenados em cache em uma fila curta para que a estratégia possa acessar as barras atuais e anteriores especificadas por
`SignalShift` sem chamar métodos indicadores como `GetValue`, obedecendo às diretrizes do repositório.
- A execução da negociação usa `BuyMarket`/`SellMarket` quando a estratégia é plana; quando existe uma posição na direção oposta,
`ClosePosition` é emitido primeiro, mantendo o fluxo de pedidos idêntico ao do robô original.
- Todos os logs de tempo de execução são escritos em inglês para manter uma trilha de auditoria clara.

## Notas de conversão

- As distâncias take-profit e stop-loss multiplicam o instrumento `PriceStep`, replicando o comportamento MetaTrader `Point`.
- O volume padrão é `0.1`, o mesmo que a entrada `Lots` na fonte MQL.
- Os limites de RSI são codificados em 50 para espelhar a implementação original.
