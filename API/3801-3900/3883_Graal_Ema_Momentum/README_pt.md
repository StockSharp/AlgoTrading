# Graal EMA Estratégia de Momento
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma conversão do consultor especialista MetaTrader 4 **0Graal-CROSSmuvingi**. Ele negocia reversões de tendência que ocorrem quando uma média móvel exponencial rápida (EMA) nos preços de fechamento cruza uma EMA mais lenta calculada nos preços de abertura. Um oscilador de impulso confirma a direção do rompimento e um take-profit de distância fixa replica o modelo de execução MT4 original.

## Ideia de Negociação

1. **EMA rápido no fechamento** rastreia a ação de preço mais recente.
2. **EMA lenta em aberto** fica para trás e forma a linha de base do cruzamento.
3. **Oscilador de impulso (período 14)** mede a intensidade com que o preço acelera para longe do valor neutro (100). A estratégia só é negociada quando o momentum se desvia de 100 em mais do que um filtro configurável e continua a fortalecer-se na mesma direção.
4. **Take Profit** fecha negociações após uma distância predefinida medida em pontos do instrumento, espelhando o parâmetro MT4 `TakeProfit`.

## Regras de entrada

- **Configuração longa**
  - O EMA rápida cruza acima do EMA lenta na vela finalizada atual, enquanto a barra anterior tinha o EMA rápida abaixo ou igual ao EMA lenta.
  - O momento (valor menos 100) é maior que o limite `MomentumFilter` e também maior que a leitura do momento da barra anterior.
  - As posições curtas existentes são fechadas antes de abrir uma nova posição longa. O novo tamanho comprado é igual ao `Volume` configurado mais qualquer valor necessário para abrir uma posição vendida.
- **Configuração curta**
  - A rápida EMA cruza abaixo da lenta EMA enquanto a barra anterior tinha a rápida EMA acima ou igual à lenta EMA.
  - O momento (valor menos 100) está abaixo do limite negativo `MomentumFilter` e menor que a leitura do momento da barra anterior.
  - As posições longas existentes são fechadas antes de abrir uma nova posição curta. O novo tamanho vendido é igual ao `Volume` configurado mais a quantidade necessária para cobrir uma posição comprada em aberto.

## Regras de saída

- As posições são fechadas automaticamente quando o preço atinge a meta de lucro calculada (`TakeProfitPoints * PriceStep`).
- Um novo sinal oposto também inverte a posição imediatamente porque o tamanho da ordem sempre inclui a quantidade da posição atual.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `FastPeriod` | Comprimento do EMA nos preços de fechamento. | 13 |
| `SlowPeriod` | Comprimento do EMA nos preços de abertura. | 34 |
| `MomentumPeriod` | Lookback do oscilador de momento. | 14 |
| `MomentumFilter` | Desvio mínimo de impulso absoluto de 100 necessário para negociar. | 0,1 |
| `TakeProfitPoints` | Distância até a meta de lucro em faixas de preço (multiplicada por `PriceStep`). | 200 |
| `CandleType` | Tipo de dados Candle usado para cálculos (período de 15 minutos por padrão). | Período de 15 minutos |
| `Volume` | Tamanho do pedido usado para novas entradas. O mecanismo o herda da classe base. | 1 |

## Notas de implementação

- Os sinais são processados apenas em velas fechadas (`CandleStates.Finished`).
- A estratégia assina o tipo de vela escolhido com `SubscribeCandles` e vincula EMA e indicadores de impulso por meio do API de alto nível.
- O EMA lenta é atualizado manualmente com preços de abertura dentro do callback de ligação para replicar o comportamento do MT4 onde `PRICE_OPEN` foi usado.
- O gerenciamento de lucro observa altos e baixos intrabares para emular a lógica de saída baseada em pontos do MT4.
- `StartProtection()` é ativado no início para proteger contra posições abertas inesperadas antes que a estratégia comece a ser negociada.
