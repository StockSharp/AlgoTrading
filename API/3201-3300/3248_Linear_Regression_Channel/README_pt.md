# Estratégia de Linear Regression Channel (Fibo)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é a conversão StockSharp do consultor especialista MetaTrader **"linear regression channel"**. Opera na direção da tendência linear de período superior confirmada por um conjunto de médias móveis ponderadas, leituras de Momentum e um filtro MACD mensal. As regras de gestão de dinheiro replicam o comportamento original com metas de lucro flutuantes, trailing de ganhos acumulados, proteção de ponto de equilíbrio e um stop de capital.

## Lógica de trading
1. **Período principal** – tipo de candle configurável (padrão 15 minutos). Todos os cálculos de sinal são executados neste período.
2. **Filtro de tendência** – uma LWMA rápida e uma lenta calculadas sobre o preço típico. Sinais longos requerem que a LWMA rápida esteja acima da lenta; sinais curtos requerem o oposto.
3. **Confirmação de Momentum** – o indicador de Momentum é avaliado em um período superior que espelha o mapeamento original do MetaTrader (M1→M15, M5→M30, M15→H1, M30→H4, H1→D1, H4→W1, D1→MN1). Os últimos três valores de Momentum são convertidos para a distância absoluta do nível 100. Uma configuração longa precisa que qualquer uma das três distâncias exceda o limiar altista, enquanto uma configuração curta precisa que qualquer uma das três exceda o limiar baixista.
4. **Viés MACD mensal** – candles mensais alimentam um filtro MACD(12,26,9). Trades longos só são permitidos quando a linha principal do MACD está acima de sua linha de sinal; trades curtos requerem a relação oposta.
5. **Condição de entrada** – quando todos os filtros se alinham e o trading é permitido, a estratégia abre uma ordem de mercado na direção correspondente. A posição atual é fechada e revertida quando um sinal oposto é produzido.

## Gestão de risco e trades
- **Stop-loss / take-profit fixo** – distâncias são definidas em pontos do instrumento e aplicadas a cada entrada. Se o máximo/mínimo do candle perfurar esses níveis, a posição é fechada.
- **Trailing stop** – opcional; ativa quando a posição ganha uma quantidade configurável de pontos e segue o melhor preço com o offset especificado.
- **Ponto de equilíbrio** – opcional; após o preço avançar pela distância de acionamento, o nível de stop é movido para o preço de entrada mais/menos um offset para garantir lucros.
- **Take-profit de lucro flutuante** – meta monetária opcional. Quando o lucro flutuante líquido (expresso em moeda da conta) excede o limiar, todas as posições são fechadas.
- **Take-profit baseado em percentual** – meta opcional baseada no capital inicial no momento em que a estratégia inicia.
- **Trailing monetário** – uma vez que o lucro flutuante atinge o acionador, a estratégia registra o lucro máximo. Se o lucro recuar pelo valor de stop especificado, a posição é fechada.
- **Stop de capital** – proteção contra drawdown opcional. Enquanto a posição está perdendo, se a perda flutuante exceder uma porcentagem do pico de capital observado, a estratégia liquida a posição.

## Parâmetros
| Nome | Descrição |
| ---- | ----------- |
| `Candle Type` | Período principal para geração de sinais. |
| `Fast LWMA` / `Slow LWMA` | Períodos para as médias móveis ponderadas linealmente rápida e lenta. |
| `Momentum Length` | Comprimento de retrospectiva do Momentum no período superior. |
| `Momentum Buy Threshold` / `Momentum Sell Threshold` | Distância absoluta mínima de 100 necessária para confirmação de Momentum altista/baixista. |
| `Take Profit (points)` / `Stop Loss (points)` | Distâncias de proteção expressas em pontos do instrumento. |
| `Use Trailing`, `Trailing Activation`, `Trailing Offset` | Configuração do trailing stop. |
| `Use Break-even`, `Break-even Trigger`, `Break-even Offset` | Parâmetros de lógica de ponto de equilíbrio. |
| `Max Trades` | Número máximo de entradas sequenciais permitidas durante a execução. |
| `Order Volume` | Volume base para ordens de mercado. |
| `Use Money TP`, `Money Take Profit` | Take-profit monetário flutuante. |
| `Use Percent TP`, `Percent Take Profit` | Take-profit calculado como percentual do capital inicial. |
| `Enable Money Trailing`, `Money Trailing Trigger`, `Money Trailing Stop` | Trailing do lucro flutuante. |
| `Use Equity Stop`, `Equity Risk %` | Proteção de stop-loss baseada em capital. |

## Notas
- A estratégia mantém apenas uma posição líquida (longa ou curta) e reverte quando um sinal oposto chega.
- As assinaturas de Momentum e MACD adicionam automaticamente os períodos superiores necessários ao feed de dados através de `GetWorkingSecurities()`.
- Todos os comentários dentro do código estão em inglês conforme as diretrizes do repositório.
