# Estratégia Kijun-Sen Robot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Kijun-Sen Robot** é uma conversão direta do consultor especialista do MetaTrader 5 "Kijun Sen Robot" para a API de estratégias de alto nível do StockSharp. Opera por padrão em velas de 30 minutos e foca em cruzamentos de preço com a linha Kijun-sen do Ichimoku confirmados por uma média móvel linearmente ponderada (LWMA) de 20 períodos. A estratégia mantém a ideia original do especialista de operar apenas durante as horas mais ativas, aplicando proteção de posição com lógica dinâmica de stop, break-even e trailing.

## Indicadores e dados
- **Ichimoku** com Tenkan, Kijun e Senkou Span B configurados em 6/12/24 períodos.
- **Média Móvel Linearmente Ponderada (LWMA)** de 20 barras para confirmação de inclinação e filtragem de distância.
- **Velas de 30 minutos** (padrão) para geração de sinais. Qualquer outro período pode ser selecionado através do parâmetro `CandleType`.

## Lógica de trading
### Entrada comprada
1. A vela atravessa a linha Kijun de baixo para cima. A vela deve ou abrir abaixo da linha, fechar acima dela, ou tocá-la durante a barra enquanto o fechamento anterior também estava abaixo.
2. O Kijun atual é plano ou subindo comparado com duas barras atrás.
3. A LWMA está pelo menos `MaFilterPips` (convertido em unidades de preço) abaixo do nível Kijun, mantendo a linha base acima da média móvel.
4. A inclinação da LWMA é positiva (LWMA atual maior que o valor anterior).
5. O horário de trading está dentro de `[TradingStartHour, TradingEndHour)`, padrão 07:00–19:00 hora da bolsa.

Quando todas as condições são satisfeitas e a estratégia não está já líquida comprada, uma ordem de compra a mercado é enviada (qualquer vendido existente é coberto primeiro). O preço de entrada é o fechamento da vela.

### Entrada vendida
1. A vela atravessa a linha Kijun de cima para baixo (espelho da lógica comprada).
2. O Kijun é plano ou caindo relativo a duas barras atrás.
3. A LWMA está pelo menos `MaFilterPips` acima do nível Kijun.
4. A inclinação da LWMA é negativa (LWMA atual menor que o valor anterior).
5. A entrada ocorre apenas dentro da janela de trading permitida.

Uma ordem de venda a mercado é colocada (a exposição comprada existente é fechada antes de abrir uma posição vendida).

### Gestão de posição e saídas
- **Stop-loss inicial** – colocado `StopLossPips` abaixo/acima do preço de entrada (convertido em unidades de preço via o passo de preço do instrumento). Isso reproduz o stop protetor da versão MQL.
- **Movimento break-even** – uma vez que o lucro não realizado excede `BreakEvenPips`, o stop é movido para o preço de entrada mais um pip (comprado) ou menos um pip (vendido). O limiar é medido usando a mesma lógica de conversão de pips.
- **Trailing stop** – após o preço avançar `TrailingStopPips`, o stop segue o preço a essa distância, apenas na direção favorável.
- **Take-profit fixo** – alvo opcional definido por `TakeProfitPips`. Definir como zero para desabilitar.
- **Saída por inclinação Kijun** – se a LWMA virar contra a operação antes de o stop se mover além do break-even, a posição é fechada imediatamente, correspondendo à saída de emergência do especialista original.
- **Filtro de tempo** – novas operações são ignoradas fora da janela configurada, mas operações abertas continuam sendo gerenciadas até serem fechadas pelas regras acima.
- **Tratamento de ordens** – a estratégia StockSharp usa exclusivamente ordens a mercado; a lógica complexa de entrada limite-vs-mercado do EA original é simplificada porque dados de velas são usados em vez de dados de tick.

Se tanto o nível de stop-loss quanto o de take-profit seriam violados dentro da mesma barra, o stop-loss tem precedência para permanecer conservador sem informação intrabar.

## Parâmetros
| Parâmetro | Valores padrão | Descrição |
|-----------|---------|-------------|
| `TenkanPeriod` | 6 | Comprimento do Ichimoku Tenkan-sen. |
| `KijunPeriod` | 12 | Comprimento do Ichimoku Kijun-sen. |
| `SenkouSpanBPeriod` | 24 | Comprimento do Ichimoku Senkou Span B. |
| `LwmaPeriod` | 20 | Comprimento do filtro de confirmação LWMA. |
| `MaFilterPips` | 6 | Distância mínima LWMA-para-Kijun em pips. |
| `StopLossPips` | 50 | Distância do stop protetor inicial. |
| `BreakEvenPips` | 9 | Lucro necessário para mover o stop para break-even. |
| `TrailingStopPips` | 10 | Distância para o movimento do trailing stop. |
| `TakeProfitPips` | 120 | Distância opcional de take-profit fixo. |
| `TradingStartHour` | 7 | Primeira hora de trading permitida (inclusiva). |
| `TradingEndHour` | 19 | Última hora de trading permitida (exclusiva). |
| `CandleType` | Período de 30 minutos | Tipo de dados usado para avaliação de sinais. |

Todos os parâmetros baseados em pips são traduzidos em unidades de preço usando o `PriceStep` do instrumento. Instrumentos com 3 ou 5 dígitos decimais recebem automaticamente um fator de 10 para replicar o tamanho clássico de pip forex.

## Notas de implementação
- A conversão mantém as variáveis de estado da estratégia (comportamento `longcross`, `shortcross`) via `_pendingLongLevel` e `_pendingShortLevel`, garantindo que novas posições requeiram um novo cruzamento Kijun.
- Verificações intrabar como "último bid/ask" da versão MT5 são aproximadas com condições no nível de vela (`Open`, `Close`, `High`, `Low`). Isso torna a lógica determinística para backtesting no StockSharp.
- A proteção de posição usa `ClosePosition()` e rastreamento manual de stop em vez de modificações de ordens MT5. Os ajustes de break-even e trailing são executados uma vez por vela finalizada.
- O método auxiliar `ConvertPips` realiza a conversão pip-para-preço usando `Security.PriceStep` ou `Security.MinPriceStep`, aplicando um multiplicador 10× para tamanhos de tick de 3 ou 5 decimais para emular a regra `digits_adjust` do MT5.
- Como a estratégia está vinculada à API de alto nível, os indicadores são vinculados via `SubscribeCandles().BindEx(...)`, e os desenhos do gráfico são configurados automaticamente (velas, Ichimoku, LWMA, negociações próprias).

## Diretrizes de uso
1. Anexar a estratégia a um instrumento que suporte velas de 30 minutos (ou configurar um `CandleType` diferente).
2. Configurar `Volume` na instância da estratégia para o tamanho de ordem desejado antes de iniciar.
3. Opcionalmente ajustar os parâmetros baseados em pips para refletir a volatilidade do instrumento ou reproduzir configurações otimizadas para pares de moedas específicos.
4. Executar no backtester de alto nível ou ambiente ao vivo; a estratégia aplicará a mesma janela de trading, regras de stop e trailing que o especialista original.
5. Monitorar o log ou gráfico para ver as atualizações de break-even e trailing. Todos os comentários no código estão em inglês para clareza, conforme solicitado.

A versão Python é omitida intencionalmente; apenas a implementação em C# é fornecida nesta pasta.
