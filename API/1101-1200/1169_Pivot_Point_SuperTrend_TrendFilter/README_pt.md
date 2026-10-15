# Estratégia Pivot Point SuperTrend com Filtro de Tendência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Combina uma linha SuperTrend baseada em pivôs com um filtro de tendência SuperTrend e uma confirmação de média móvel. Opera quando a tendência muda ou quando um sinal de Pivot SuperTrend aparece dentro de uma janela de datas.

## Detalhes

- **Critérios de entrada**:
  - Compra: o SuperTrend do preço muda para alta e o fechamento supera a SMA dos fechamentos, ou o SuperTrend de pivôs muda para alta no intervalo de datas.
  - Venda a descoberto: o SuperTrend do preço muda para baixa e o fechamento fica abaixo da SMA, ou o SuperTrend de pivôs muda para baixa no intervalo de datas.
- **Critérios de saída**:
  - Qualquer mudança para baixa fecha toda a posição comprada; qualquer mudança para alta fecha toda a posição vendida, independentemente de SMA e datas.
  - Inversão apenas com entrada oposta confirmada. Mudanças simultâneas opostas fecham a posição sem nova entrada.
- **Stops**: Nenhum
- **Valores padrão**:
  - `PivotPeriod` = 2
  - `Factor` = 3
  - `AtrPeriod` = 10
  - `TrendAtrPeriod` = 10
  - `TrendMultiplier` = 3
  - `MaPeriod` = 20
  - `CandleType` = 5 minutos
  - `StartDate` / `EndDate` = UTC `DateTime.MinValue` / `DateTime.MaxValue`
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Pivot, SuperTrend, SMA
  - Stops: Não
  - Complexidade: Moderado
  - Período: Qualquer
  - Sazonalidade: Opcional
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

## Cálculo e execução

Usam-se apenas velas concluídas. Um pivô máximo/mínimo deve ficar estritamente acima/abaixo de todas as `PivotPeriod` velas de cada lado e só é confirmado após as velas à direita terminarem. Se ambos ocorrerem na mesma vela, o máximo tem prioridade. O primeiro pivô inicializa `center`; os demais usam `(2 * center + pivot) / 3`.

As bandas usam Wilder ATR: média dos primeiros `AtrPeriod` true ranges, depois suavização com peso `1 / AtrPeriod`. Suporte/resistência básicos são `center ± Factor * ATR`. O suporte conserva o máximo entre valor básico e anterior quando o fechamento anterior supera o suporte anterior; caso contrário reinicia. A resistência conserva o mínimo quando o fechamento anterior fica abaixo da resistência anterior; caso contrário reinicia. Fechamento estritamente acima da resistência anterior muda para alta; abaixo do suporte anterior muda para baixa; caso contrário mantém a direção. A direção inicial é de alta; inicialização não gera sinal.

O filtro de preço usa o SuperTrend nativo do StockSharp (`TrendAtrPeriod`, `TrendMultiplier`). A SMA inclui o fechamento atual. Só mudanças de direção geram sinais; os dois ATRs, SMA e centro pivô devem estar prontos. `StartDate <= candle.OpenTime <= EndDate` limita apenas entradas pivô. As datas devem estar ordenadas em UTC. Entrada: `Volume`; ordem de inversão: `Volume + abs(Position)`; fechamento: `abs(Position)`. Sem espera ou stops protetores.

Conceito pivô: [Pivot Point Supertrend de LonesomeTheBlue](https://www.tradingview.com/script/L0AIiLvH-Pivot-Point-Supertrend/). As regras acima definem o modelo bidirecional deste exemplo.
