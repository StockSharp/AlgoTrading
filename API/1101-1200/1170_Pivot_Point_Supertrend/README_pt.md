# Estratégia Pivot Point Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina Pivot Points com um Supertrend baseado em ATR para capturar reversões de tendência.

Os Pivot Points definem uma linha central dinâmica. Um multiplicador ATR constrói bandas superior e inferior que seguem o preço. Quando a tendência muda de direção, a estratégia entra de acordo.

## Detalhes

- **Critérios de entrada**: Sinais baseados em Pivot Points e ATR Supertrend.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `PivotPeriod` = 2
  - `AtrFactor` = 3m
  - `AtrPeriod` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Pivot Points, ATR
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

## Cálculo e execução

Usam-se apenas velas concluídas. Um pivô máximo/mínimo deve ficar estritamente acima/abaixo de todas as `PivotPeriod` velas de cada lado e só é confirmado após as velas à direita terminarem. Se ambos ocorrerem na mesma vela, o máximo tem prioridade. O primeiro pivô inicializa `center`; os demais usam `(2 * center + pivot) / 3`.

As bandas usam Wilder ATR: média dos primeiros `AtrPeriod` true ranges, depois suavização com peso `1 / AtrPeriod`. Suporte/resistência básicos são `center ± AtrFactor * ATR`. O suporte conserva o máximo entre valor básico e anterior quando o fechamento anterior supera o suporte anterior; caso contrário reinicia. A resistência conserva o mínimo quando o fechamento anterior fica abaixo da resistência anterior; caso contrário reinicia. Fechamento estritamente acima da resistência anterior muda para alta; abaixo do suporte anterior muda para baixa; caso contrário mantém a direção. A direção inicial é de alta; inicialização não gera sinal.

Wilder ATR e um centro pivô confirmado devem estar prontos. Só a mudança de direção das bandas gera entrada: alta abre compra, baixa abre venda a descoberto. A direção inicial é de alta; a inicialização não gera ordem. Cada sinal oposto inverte toda a posição com uma ordem a mercado de `Volume + abs(Position)`; entrada sem posição usa `Volume`. Sem SMA, SuperTrend de preço separado, filtro de datas, espera ou stop protetor.

Conceito pivô: [Pivot Point Supertrend de LonesomeTheBlue](https://www.tradingview.com/script/L0AIiLvH-Pivot-Point-Supertrend/). As regras acima definem o modelo bidirecional deste exemplo.
