# Estratégia Hans123 Trader v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Hans123 Trader v2 é uma estratégia de rompimento que coloca ordens de stop pendentes em torno do range de trading recente. Espelha a implementação do MetaTrader por Vladimir Karputov e está adaptada para a API de alto nível do StockSharp. O sistema se concentra em capturar momentum quando o preço escapa do range mais recente de 80 barras, enquanto gerencia saídas protetoras e um trailing stop.

## Ideia central

- Monitorar uma série de candles configurável (barras de 1 hora por padrão).
- Durante a janela de sessão ativa, calcular a máxima mais alta e a mínima mais baixa ao longo dos últimos *N* candles (80 por padrão).
- Colocar uma ordem de buy stop na máxima mais alta e uma ordem de sell stop na mínima mais baixa quando o mercado estiver longe o suficiente do bid/ask atual.
- Limitar o número total de ordens pendentes ativas para evitar sobreexposição.
- Uma vez que uma posição é aberta, cancelar as ordens pendentes restantes, aplicar offsets de stop-loss e take-profit (medidos em pips), e ativar um trailing stop.

## Gestão de operações

- **Entradas**: Ordens de stop são colocadas apenas enquanto o tempo do candle processado cair entre as horas de início e fim configuradas. As ordens são ignoradas fora dessa janela.
- **Proteção de posição**: Quando uma nova posição é criada, a estratégia registra imediatamente ordens protetoras de stop-loss e take-profit usando as distâncias de pip configuradas.
- **Trailing stop**: Se habilitado, a ordem de stop-loss é reemitida mais perto do preço uma vez que ele se move a favor da posição por mais do que o limiar de trailing mais o passo.
- **Limpeza de ordens**: Sair de uma posição cancela as ordens protetoras, e qualquer nova entrada cancela as ordens pendentes opostas, correspondendo ao comportamento da lógica MQL original.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `Volume` | Tamanho de ordem usado ao enviar ordens de rompimento e protetoras. |
| `StopLossPips` | Distância em pips entre o preço de entrada e o stop-loss protetor. Definir como `0` para desabilitar. |
| `TakeProfitPips` | Distância em pips entre o preço de entrada e a ordem de take-profit. Definir como `0` para desabilitar. |
| `TrailingStopPips` | Distância inicial do trailing stop em pips. `0` desabilita o trailing. |
| `TrailingStepPips` | Lucro adicional mínimo em pips necessário antes de mover o trailing stop novamente. Deve ser diferente de zero quando o trailing estiver habilitado. |
| `StartHour` | Hora de abertura da sessão (inclusive) para colocar novas ordens pendentes. |
| `EndHour` | Hora de fechamento da sessão (exclusive) para colocar novas ordens pendentes. Deve ser maior que `StartHour`. |
| `MaxPendingOrders` | Número máximo de ordens de rompimento simultâneas (compra + venda) permitidas. |
| `BreakoutPeriod` | Comprimento de retrocesso (em candles) para os cálculos de máxima mais alta e mínima mais baixa. |
| `CandleType` | Série de candles processada pela estratégia (período ou outro tipo de dado de candle). |

## Notas

- O tamanho do pip é derivado do passo de preço do instrumento. Para símbolos forex de 3 e 5 dígitos, o valor do ponto é ajustado para corresponder à definição MQL de um pip.
- A estratégia depende dos snapshots `Security.BestBid`/`BestAsk` quando disponíveis. Se dados de profundidade não estiverem presentes, ela recorre ao preço de fechamento do candle atual para avaliar a distância mínima do mercado.
- As ordens protetoras são recriadas sempre que precisam ser movidas, refletindo a lógica `PositionModify` do expert advisor original.
- A implementação mantém a lógica puramente em C# sem tradução para Python, como solicitado.
