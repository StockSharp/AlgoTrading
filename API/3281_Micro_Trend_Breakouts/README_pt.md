# Estratégia Micro Trend Breakouts
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia **Micro Trend Breakouts** é uma conversão do expert advisor do MetaTrader "Micro Trend Breakouts" para a API de alto nível do StockSharp. Ela detecta padrões de rompimento de curta duração usando médias móveis ponderadas lineares, picos de momentum e alinhamento MACD. A estratégia abre no máximo uma posição por vez e depende dos preços de fechamento dos candles para acionar entradas e saídas.

## Indicadores
- **Médias móveis ponderadas lineares (LWMA)** - Médias rápida e lenta construídas no período de análise filtram a direção dominante do mercado.
- **Momentum** - Leituras absolutas de momentum nos três últimos candles concluídos devem exceder um limite configurável para confirmar que o preço acelera na direção do rompimento.
- **MACD** - O histograma MACD clássico é usado como filtro direcional (linha principal acima do sinal para compradas e abaixo do sinal para vendidas).

## Lógica de entrada
1. Aguarde um candle finalizado do período configurado.
2. Exija que a LWMA rápida esteja acima da LWMA lenta para compradas (abaixo para vendidas).
3. Confirme uma pequena estrutura de rompimento: a mínima do candle de duas barras atrás deve estar abaixo da máxima do candle anterior para compradas (espelhado para vendidas).
4. Exija aceleração de momentum: qualquer um dos três últimos valores absolutos de momentum deve exceder o limite configurado.
5. Valide o alinhamento MACD:
   - Compradas: a linha principal MACD deve estar acima da linha de sinal, independentemente de estar acima ou abaixo de zero.
   - Vendidas: a linha principal MACD deve estar abaixo da linha de sinal, independentemente da posição da linha zero.

Quando todas as verificações concordam, a estratégia emite uma ordem a mercado usando o parâmetro de volume padrão.

## Lógica de saída e gestão de risco
- Níveis iniciais de stop-loss e take-profit são expressos em passos de preço e calculados na entrada. Definir um valor como zero desabilita o nível correspondente.
- Um módulo opcional de breakeven move o stop em direção ao preço de entrada depois que o preço avança uma quantidade configurada de passos, adicionando opcionalmente uma margem de segurança.
- A proteção trailing pode apertar o stop depois de um movimento lucrativo. Quando o lucro excede o limite de ativação, o stop é arrastado pela distância de trailing a partir do maior (para compradas) ou menor (para vendidas) preço de candle visto desde a entrada.
- Saídas de posição são avaliadas em cada candle finalizado. Se o preço atingir níveis de stop-loss ou take-profit, a estratégia fecha a posição com uma ordem a mercado e redefine o estado interno.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `Order Volume` | Volume da ordem a mercado usado para entradas. | `1` |
| `Candle Type` | Período para análise de preço. | `15m time frame` |
| `Fast LWMA` | Período da média móvel ponderada linear rápida. | `6` |
| `Slow LWMA` | Período da média móvel ponderada linear lenta. | `85` |
| `Momentum Period` | Retrospectiva do indicador de momentum. | `14` |
| `Momentum Threshold` | Momentum absoluto mínimo exigido nos três últimos candles. | `0.3` |
| `MACD Fast / Slow / Signal` | Períodos de média móvel usados pelo MACD. | `12 / 26 / 9` |
| `Stop Loss` | Distância do stop em passos de preço. `0` desabilita o stop. | `20` |
| `Take Profit` | Distância do alvo em passos de preço. `0` desabilita o alvo. | `50` |
| `Use Trailing` | Habilita a lógica de trailing stop. | `true` |
| `Trail Activation` | Lucro em passos necessário antes de o trailing stop ficar ativo. | `40` |
| `Trail Step` | Distância entre o extremo e o trailing stop em passos. | `40` |
| `Use Breakeven` | Habilita o ajuste de stop breakeven. | `true` |
| `Breakeven Trigger` | Lucro em passos que arma o módulo breakeven. | `30` |
| `Breakeven Padding` | Passos adicionais adicionados ao mover o stop para breakeven. | `30` |

## Observações
- A estratégia assina um único fluxo de candles e evita chamadas de API de baixo nível, permanecendo dentro dos requisitos do framework de alto nível.
- Ordens de proteção não são anexadas diretamente às operações; em vez disso, a estratégia usa monitoramento baseado em candles combinado com `StartProtection()` para garantir que a classe base supervisione posições abertas.
- Todos os comentários inline no código C# são escritos em inglês, conforme exigido pelas diretrizes de conversão.
