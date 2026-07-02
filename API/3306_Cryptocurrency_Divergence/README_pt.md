# Estratégia Cryptocurrency Divergence
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Cryptocurrency Divergence** procura divergências clássicas de momentum entre a ação do preço e o Relative Strength Index (RSI), confirmando a direção da tendência com médias móveis e MACD. O expert advisor MetaTrader original dependia de checagens de momentum multi-timeframe, gestão monetária e extensa lógica de trailing. Este port StockSharp mantém o espírito do sistema ao:

- Detectar divergências altistas quando o preço imprime uma mínima mais baixa, mas o RSI forma uma mínima mais alta.
- Detectar divergências baixistas quando o preço cria uma máxima mais alta, mas o RSI imprime uma máxima mais baixa.
- Validar configurações com médias móveis rápida/lenta e linha MACD versus linha de sinal.
- Gerenciar posições por stop loss, take profit, break-even e trailing stop configuráveis, expressos em passos de preço.

A estratégia foi projetada para criptomoedas spot, mas pode ser aplicada a qualquer instrumento que entregue volatilidade suficiente e pontos de swing claros.

## Indicadores
- **Média móvel simples (SMA)**: uma SMA rápida e uma lenta fornecem o filtro primário de tendência.
- **Relative Strength Index (RSI)**: fornece os valores de pivô de momentum usados para medir a força da divergência.
- **Moving Average Convergence Divergence (MACD)**: confirma que o momentum concorda com a direção da divergência detectada.

Todos os indicadores são vinculados pela API de alto nível, então nenhum buffer manual é necessário.

## Lógica de negociação
1. Assinar o tipo de candle configurado e calcular valores de SMA, RSI e MACD em cada barra concluída.
2. Acompanhar os swing highs e lows mais recentes junto com seus valores RSI. Apenas extensões monotônicas (novas máximas mais altas ou mínimas mais baixas) atualizam os dados de swing.
3. Uma **divergência altista** aparece quando uma nova mínima mais baixa no preço é combinada com uma mínima mais alta no RSI. A operação também exige que a SMA rápida esteja acima da lenta, que a linha MACD exceda a linha de sinal e que o RSI permaneça abaixo do nível neutro (padrão 45) para garantir condições de sobrevenda.
4. Uma **divergência baixista** exige uma nova máxima mais alta no preço com uma máxima mais baixa no RSI, SMA rápida abaixo da lenta, linha MACD abaixo do sinal e RSI acima do nível baixista neutro (padrão 55).
5. A estratégia abre apenas uma posição líquida por vez. Reversões fecham a posição existente e entram imediatamente na direção oposta quando os sinais se alinham.

## Gestão de risco
- **Volume**: tamanho de operação definido pelo usuário e aplicado a todas as ordens a mercado.
- **Stop Loss / Take Profit**: expressos em passos de preço e anexados após cada execução usando o preço real de execução.
- **Movimento de break-even**: opcionalmente substitui o stop loss por um offset acima/abaixo da entrada quando o preço percorre uma distância configurável.
- **Trailing Stop**: opcionalmente acompanha atrás do preço de fechamento a uma distância fixa medida em passos. O trailing stop tem prioridade sobre o stop loss original após a ativação.

Stops e metas são avaliados em cada candle concluído, garantindo comportamento determinístico que corresponde entre backtests e execução em tempo real.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `CandleType` | Série de candles usada para análise (timeframe de 15 minutos por padrão). |
| `TradeVolume` | Volume de ordem aplicado a todas as entradas. |
| `FastMaLength` / `SlowMaLength` | Períodos das SMAs rápida e lenta. |
| `RsiLength` | Comprimento de cálculo do RSI. |
| `RsiBullishLevel` / `RsiBearishLevel` | Limiares RSI que definem zonas de sobrevenda e sobrecompra para confirmação de divergência. |
| `MacdShortLength` / `MacdLongLength` / `MacdSignalLength` | Configuração MACD. |
| `StopLossPoints` / `TakeProfitPoints` | Distâncias em passos de preço para risco e metas de recompensa. |
| `EnableBreakEven`, `BreakEvenTrigger`, `BreakEvenOffset` | Controles para o movimento de break-even. |
| `EnableTrailing`, `TrailDistance` | Ativação e espaçamento do trailing stop. |

Cada parâmetro é exposto por `StrategyParam<T>` para poder ser otimizado dentro do designer StockSharp.

## Notas de uso
1. Anexe a estratégia a um símbolo de criptomoeda e garanta que o instrumento tenha `PriceStep` e `Board` definidos. Sem passo de preço, a estratégia não consegue calcular stops.
2. Alinhe o tipo de candle ao mercado negociado (por exemplo, 15m, 1h). A detecção de divergência é sensível ao timeframe.
3. Ajuste as distâncias de stop e alvo à volatilidade do instrumento. Pares cripto com cinco casas decimais costumam exigir contagens de passos maiores.
4. Habilite break-even ou trailing apenas após observar colchão de lucro suficiente em testes históricos; trailing agressivo pode encerrar operações prematuramente.
5. Monitore a estratégia no designer StockSharp ou no painel de dados de mercado para visualizar alinhamento de indicadores e operações executadas.

## Diferenças em relação à versão MQL
- Trailing baseado em dinheiro e proteções de stop de patrimônio são simplificados em gestão de stop baseada em passos de preço.
- Checagens de momentum multi-timeframe são substituídas por confirmação MACD em um único timeframe para clareza.
- Efeitos colaterais de e-mail/notificação são omitidos porque são tratados externamente nos ecossistemas StockSharp.

Apesar desses ajustes, a detecção central de divergência e a lógica protetora permanecem fiéis à intenção do expert advisor original.
