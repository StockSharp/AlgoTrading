# Estratégia de Crypto Scalper
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Crypto Scalper reproduz a lógica do especialista MetaTrader original com componentes de alto nível do StockSharp. Observa um cruzamento altista ou baixista de uma média móvel ponderada linear rápida no período principal e confirma a configuração com filtros de tendência calculados em um período superior. Uma vez que as condições se alinham, a estratégia entra usando ordens de mercado e gerencia saídas através de distâncias de stop-loss e take-profit medidas em pips do MetaTrader.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `Primary Candle` | Tipo de vela processado no período principal. | Período de 1 minuto |
| `Higher Candle` | Tipo de vela de período superior usado para confirmação. | Período de 15 minutos |
| `Fast LWMA` | Comprimento da média móvel ponderada linear principal. | 8 |
| `Higher Fast MA` | Comprimento da LWMA rápida no período de confirmação. | 6 |
| `Higher Slow MA` | Comprimento da LWMA lenta no período de confirmação. | 85 |
| `Momentum Period` | Comprimento do indicador de Momentum aplicado às velas do período superior. | 14 |
| `Momentum Threshold` | Desvio mínimo do momentum de referência (linha de base MetaTrader 100) necessário para negociar. | 0.3 |
| `Momentum Reference` | Nível de referência usado para emular o escalonamento de momentum do MetaTrader. | 100 |
| `Stop Loss (pips)` | Distância de stop de proteção em pips do MetaTrader. | 20 |
| `Take Profit (pips)` | Distância de lucro de proteção em pips do MetaTrader. | 50 |
| `Volume` | Volume da ordem expresso em lotes. | 0.01 |
| `MACD Fast` | Período da EMA rápida para confirmação MACD. | 12 |
| `MACD Slow` | Período da EMA lenta para confirmação MACD. | 26 |
| `MACD Signal` | Período da EMA de sinal para confirmação MACD. | 9 |

## Lógica de trading
1. Subscrever ao período principal e calcular uma LWMA que reage rapidamente ao preço.
2. Detectar uma entrada quando a vela anterior cruza a LWMA para cima (comprado) ou para baixo (vendido).
3. Confirmar o cruzamento usando os filtros do período superior:
   - A LWMA rápida superior deve permanecer acima da LWMA lenta superior para entradas compradas e abaixo para entradas vendidas.
   - O histograma MACD (principal menos sinal) deve ser positivo para comprados e negativo para vendidos.
   - O momentum deve desviar do nível de referência pelo menos por `Momentum Threshold`.
4. Enviar uma ordem de mercado na direção detectada quando não há outras ordens ativas e a posição atual permite.
5. Monitorar velas subsequentes e fechar a posição quando o preço de stop-loss ou take-profit for tocado.

## Notas
- A estratégia usa subscrições de alto nível do StockSharp com `Bind`, evitando buffers de indicadores manuais.
- Os níveis de proteção são recalculados em cada vela usando o passo de preço do ativo. Um passo de fallback de `0.0001` é aplicado se o instrumento não expõe um passo de preço configurado.
- Apenas uma posição é permitida por vez. Sinais subsequentes são ignorados até que a negociação existente seja concluída.
- Todos os comentários inline dentro da implementação C# estão escritos em inglês conforme as diretrizes do repositório.
