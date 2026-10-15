# PresentTrend RMI Synergy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

RMI nativo alinhado à SMA do fechamento, seguido de stop de preço SMA/ATR que só aperta a proteção. Não afirma lucratividade.

## Indicadores e entrada

Candles concluídos de cinco minutos por padrão. RMI nativo usa `d[t]=close[t]-close[t-RmiMomentum]`, lag3. Nos últimos `RmiPeriod=21` impulsos, U é a média dos d positivos (zero nos demais), D a média dos módulos dos d negativos (zero nos demais). `RMI=100*U/(U+D)`; denominador zero não fornece entrada. É o cálculo nativo de impulso relativo com médias móveis.

SMA do fechamento e ATR Wilder nativo compartilham `SuperTrendLength=5`. ATR inclui gaps ao fechamento anterior, inicia com média crescente e depois Wilder. Após formar os três indicadores, entra somente de zero: compra se RMI>60 e close>SMA; venda se RMI<40 e close<SMA. Ordem a mercado de `Volume`. Uma posição aberta ignora novos sinais de entrada.

`Direction` é filtro opcional tipado `Sides`: `null` em C#/`None` em Python permite ambos; `Sides.Buy` apenas comprado, `Sides.Sell` apenas vendido. Filtra entradas; o stop sempre fecha o lado real. Sem códigos numéricos ou strings de direção.

## Stop móvel

`width=SuperTrendMultiplier*ATR`, multiplicador padrão4. Compra inicializa stop=`SMA-width`; cada candle posterior usa `max(previousStop,SMA-width)`. Venda inicializa `SMA+width` e usa `min(previousStop,SMA+width)`. Nunca afrouxa. Nasce da SMA/ATR do sinal, independente da execução; os nomes SuperTrend preservados configuram estas bandas centradas em SMA.

Bid positivo≤stop comprado ou ask positivo≥stop vendido fecha toda a posição real a mercado. Após atualizar também verifica close; atingir o novo nível pode fechar imediatamente. Cotações entre candles podem disparar. O novo stop vale para preço observado atual/futuro, sem aplicar ao high/low anterior do candle. SMA/ATR pode continuar atualizando proteção com RMI vazio.

É monitoramento local: proteção por cotações entre candles exige Level1 bid/ask; sem elas continuam os fechamentos. Não envia stop nativo à bolsa; execução pode diferir do limiar. Ordem pendente bloqueia duplicados, mas indicadores/stop avançam. O candle de saída não reentra; um posterior elegível pode. Sem EMA, espera de seis horas, inversão imediata por sinal oposto ou take-profit separado.

| Parâmetro | Padrão |
|---|---:|
| `RmiPeriod` | 21, positivo |
| `RmiMomentum` | 3, lag positivo do fechamento |
| `SuperTrendLength` | 5, período positivo compartilhado SMA/ATR |
| `SuperTrendMultiplier` | 4, largura ATR positiva |
| `Direction` | Sem valor: ambos |
| `CandleType` | 5 minutos |

Testes C# e Python calculam independentemente somas de impulso, SMA, ATR Wilder e stops no arquivo BTC/TON existente. Verificam cada entrada, saída a preço executável e posição real, ambos os lados/filtros, sinais opostos mantidos, stops apertados entre candles e entradas com menos de seis horas.
