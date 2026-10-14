# Estratégia Powertrend - Filtro de Intervalo por Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa um filtro de intervalo ajustado por volume para definir bandas de preços dinâmicas. Entradas compradas ocorrem quando o preço rompe a banda superior durante uma tendência de alta, e as posições são fechadas quando o preço cai abaixo da banda inferior. Filtros opcionais incluem ADX, confirmação de tendência por máximas/mínimas e uma VWMA do valor do filtro.

## Cálculo e execução

C# e Python usam esta definição causal explícita. Nas velas concluídas, `d = abs(Close - Close anterior)` e `V = TotalVolume`. Após `RangePeriod` mudanças, `r = RangeMultiplier * sum(d * V) / sum(V)` na janela que inclui a vela atual, com volume total positivo. O primeiro filtro formado `f` é o fechamento atual; depois limite o `f` anterior ao intervalo `[Close - r, Close + r]`. As bandas de decisão são `f anterior + r` e `f anterior - r`, antes de atualizar o centro, evitando redefinir a banda como o próprio preço de rompimento após o movimento do filtro.

Entre com `Volume` apenas sem posição quando `Close > banda superior` e o filtro sobe. Feche toda a compra a mercado quando `Close < banda inferior`, independentemente dos filtros de entrada. Igualdade mantém a posição. Uma ordem pendente bloqueia ordens adicionais. Não há vendas a descoberto, cruzamento EMA ou intervalo obrigatório.

O ADX opcional precisa estar formado e estritamente acima de `AdxThreshold`; desativado, não é registrado nem exige aquecimento. A confirmação de máximas/mínimas exige simultaneamente `High atual > High anterior` e `Low atual > Low anterior`. A VWMA do filtro é `sum(f * V) / sum(V)` dos últimos `VwmaPeriod` valores formados, incluindo o atual; exige janela completa, volume total positivo e `Close > VWMA`. Filtros desativados não exigem formação. São decisões a mercado no fechamento, não ordens stop da bolsa nem execução intrabar presumida nas máximas/mínimas.

Padrões: `RangePeriod=40`, `RangeMultiplier=1.5`, `AdxPeriod=14`, `AdxThreshold=20`, `VwmaPeriod=20`, `CandleType=TimeSpan.FromMinutes(5).TimeFrame()`. `IsAdxFilterEnabled`, `IsHighLowFilterEnabled`, `IsVwmaFilterEnabled` são `false`. Períodos e multiplicador positivos; limiar ADX em `[0, 100]`. Fórmula e valores definem este exemplo; a descrição original não especificava a fórmula do indicador original.
