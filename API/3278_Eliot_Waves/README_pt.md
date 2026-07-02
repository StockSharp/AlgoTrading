# Estratégia Eliot Waves
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia Eliot Waves replica o comportamento do expert advisor do MetaTrader "Eliot Waves" usando a API de alto nível do StockSharp. O algoritmo combina detecção de tendência por duas médias móveis ponderadas lineares com confirmação de momentum e saídas baseadas em volatilidade. Todos os cálculos são realizados em candles concluídos de um período configurável para espelhar a execução determinística do robô original.

## Lógica de negociação

1. **Filtro de tendência.** A estratégia compara uma LWMA rápida (período padrão 6) com uma LWMA lenta (período padrão 85) calculadas sobre o preço típico do candle. Operações compradas são consideradas apenas quando a LWMA rápida fecha acima da LWMA lenta, enquanto operações vendidas exigem o alinhamento oposto.
2. **Confirmação de momentum.** Um indicador de momentum (período padrão 14) deve mostrar pelo menos uma das três últimas leituras se desviando do valor neutro 100 por mais que o limite configurado (padrão 0.3). Isso replica o EA original, que verificava a diferença absoluta de três valores recentes de momentum.
3. **Filtro de estrutura do candle.** Sinais comprados exigem que a mínima do candle de duas barras atrás esteja abaixo da máxima do candle anterior. Sinais vendidos exigem que a mínima do candle anterior permaneça abaixo da máxima de duas barras atrás. Isso captura o filtro estilo divergência presente no código-fonte.
4. **Escalonamento de posição.** Cada sinal tenta adicionar um passo de volume fixo (padrão 0.1) até o número máximo de passos (padrão 10). A estratégia fecha qualquer exposição oposta antes de abrir uma nova posição para permanecer alinhada com a implementação do MetaTrader.

## Gestão de risco

- **Stop-loss e take-profit.** Alvos de preço são definidos em pips relativos ao preço médio de entrada e recalculados sempre que a posição muda.
- **Trailing stop.** Quando habilitado, o stop é puxado atrás do preço quando o lucro aberto excede a distância de trailing.
- **Break-even.** Depois de atingir o gatilho configurado, o stop é movido para o preço de entrada mais um offset opcional, protegendo lucros acumulados.
- **Saída por Bollinger Band.** Posições compradas saem quando o preço toca a banda inferior de um canal Bollinger de 20 períodos, enquanto posições vendidas saem no toque da banda superior. Isso espelha a lógica de fechamento baseada em volatilidade do script MQL.
- **Confirmação MACD.** Posições também são fechadas em um cruzamento de sinal MACD (12, 26, 9) contra a direção da operação, reproduzindo a saída MACD mensal do expert original.
- **Chave de saída forçada.** O parâmetro `EnableExitStrategy` permite que um operador liquide instantaneamente toda posição aberta.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `TradeVolume` | Volume usado para cada passo de posição. | 0.1 |
| `CandleType` | Período do candle empregado para todos os indicadores. | Candles de 15 minutos |
| `FastMaPeriod` / `SlowMaPeriod` | Períodos das médias móveis ponderadas lineares rápida e lenta. | 6 / 85 |
| `MomentumPeriod` | Retrospectiva de momentum usada no bloco de confirmação. | 14 |
| `MomentumThreshold` | Desvio mínimo a partir de 100 exigido para habilitar a negociação. | 0.3 |
| `StopLossPips` / `TakeProfitPips` | Distâncias de stop-loss e take-profit expressas em pips. | 20 / 50 |
| `EnableTrailing` / `TrailingStopPips` | Chave e distância para o recurso de trailing stop. | true / 40 |
| `EnableBreakEven`, `BreakEvenTriggerPips`, `BreakEvenOffsetPips` | Chave de ativação, gatilho e offset de break-even em pips. | true, 30, 30 |
| `MaxPositions` | Número máximo de passos de volume permitidos. | 10 |
| `EnableExitStrategy` | Força a estratégia a zerar a posição quando habilitado. | false |

## Notas de conversão

- A implementação StockSharp depende do pipeline de alto nível `SubscribeCandles().BindEx(...)` para processar todos os indicadores simultaneamente e operar estritamente em candles concluídos.
- A conversão de pips usa o passo de preço do ativo sempre que possível e recorre ao valor do passo de preço quando a corretora não expõe precisão de pip, correspondendo ao comportamento adaptativo da versão MetaTrader.
- A lógica de stop-loss, take-profit, trailing e break-even é gerenciada internamente em vez de usar ordens da corretora, mantendo o comportamento determinístico durante backtests.
- Chamadas de alerta, e-mail e notificação do expert MQL foram removidas, pois o StockSharp fornece seus próprios recursos de registro.

## Dicas de uso

1. Selecione o instrumento desejado e ajuste `TradeVolume` e `MaxPositions` ao tamanho da conta. Os padrões reproduzem o escalonamento conservador usado no EA.
2. Otimize `MomentumThreshold`, `StopLossPips` e `TrailingStopPips` em dados históricos se o mercado alvo apresentar características de volatilidade diferentes.
3. Ao testar múltiplos símbolos, garanta que o ativo exponha um passo de preço correto para que distâncias baseadas em pips sejam convertidas com precisão.
4. Monitore o log para o aviso *"Unable to determine pip size from security settings"*. Se ele aparecer, considere configurar o instrumento com o passo de preço correto para evitar níveis de risco incompatíveis.
