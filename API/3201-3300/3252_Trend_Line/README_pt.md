# Estratégia de Trend Line
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Resumo
A estratégia Trend Line replica a lógica central de gerenciamento de trades do especialista MetaTrader original combinando uma média móvel ponderada linearmente rápida e lenta, um filtro de Momentum e uma confirmação MACD. A conversão se concentra em componentes StockSharp de alto nível e mantém a mesma abordagem sistemática que aguarda explosões de Momentum na direção da tendência antes de entrar. Stops de proteção, metas de lucro e um trailing stop opcional em passos de preço fornecem gerenciamento de risco semelhante ao especialista original.

## Lógica de trading
1. Subscrever a série de candles configurada e calcular os seguintes indicadores:
   - Média móvel ponderada linearmente (LWMA) rápida com o `FastMaPeriod` configurável.
   - LWMA lenta com o `SlowMaPeriod` configurável.
   - Indicador Momentum com período `MomentumPeriod`. As três leituras de Momentum mais recentes são rastreadas para emular a verificação de Momentum de múltiplas barras presente na versão MQL.
   - Indicador MACD (Convergência/Divergência de Médias Móveis) com comprimentos padrão de rápida/lenta/sinal. A estratégia armazena os valores de MACD e sinal para uso posterior.
2. Entrar longo quando:
   - A LWMA rápida está acima da LWMA lenta.
   - Pelo menos um dos últimos três valores de Momentum é maior ou igual a `MomentumBuyThreshold`.
   - A linha principal MACD está acima da linha de sinal MACD.
   - Nenhuma posição curta aberta existe (a exposição curta é zerada antes de abrir uma posição longa).
3. Entrar curto quando:
   - A LWMA rápida está abaixo da LWMA lenta.
   - Pelo menos um dos últimos três valores de Momentum é menor ou igual a `MomentumSellThreshold` (o limiar deve ser negativo para detectar aceleração descendente).
   - A linha principal MACD está abaixo da linha de sinal MACD.
   - Nenhuma posição longa aberta existe (a exposição longa é zerada antes de abrir uma posição curta).
4. Após cada entrada, a estratégia coloca ordens protetoras de stop-loss e take-profit por distância em passos de preço. Ambas as ordens são recalculadas toda vez que a posição muda.
5. Um trailing stop pode ser ativado com `TrailingStopSteps` e `TrailingTriggerSteps`. Uma vez que a posição aberta ganha pelo menos a distância de acionamento, o stop-loss é movido para `TrailingStopSteps` do preço de fechamento atual do candle processado.

## Parâmetros
- `CandleType` – tipo de dados para a série de candles usada por cada indicador (padrão período de 1 hora).
- `FastMaPeriod` – período da LWMA rápida (padrão 6).
- `SlowMaPeriod` – período da LWMA lenta (padrão 85).
- `MomentumPeriod` – número de candles para o cálculo de Momentum (padrão 14).
- `MomentumBuyThreshold` – Momentum positivo mínimo necessário para permitir novas posições longas (padrão 0.3).
- `MomentumSellThreshold` – Momentum máximo (negativo) permitido antes de abrir novas posições curtas (padrão -0.3).
- `MacdFastLength` – comprimento EMA rápida do MACD (padrão 12).
- `MacdSlowLength` – comprimento EMA lenta do MACD (padrão 26).
- `MacdSignalLength` – comprimento EMA de sinal do MACD (padrão 9).
- `StopLossSteps` – distância do stop de proteção em passos do instrumento (padrão 20).
- `TakeProfitSteps` – distância da meta de lucro de proteção em passos (padrão 50).
- `TrailingStopSteps` – distância do trailing stop em passos (padrão 40, desativado quando zero).
- `TrailingTriggerSteps` – lucro em passos necessário antes de o trailing stop ficar ativo (padrão 40).

## Notas
- Os vínculos de indicadores dependem apenas de candles terminados; dados não terminados são ignorados para evitar sinais prematuros.
- `SetStopLoss` e `SetTakeProfit` trabalham com distâncias em passos de preço, o que mantém o comportamento consistente em instrumentos com diferentes tamanhos de tick.
- Quando `MomentumSellThreshold` é mantido positivo, a comparação padrão (`<= threshold`) espera que esse valor seja negativo. Ajuste o sinal ao otimizar a estratégia.
- O trailing stop funciona no modo de fim de barra porque é atualizado quando cada candle terminado é processado, espelhando o script original que recalculava os stops em novas barras.
- A conversão omite intencionalmente o desenho manual de linhas de tendência e as regras de liquidação baseadas em capital, pois dependiam de recursos interativos de terminal indisponíveis no StockSharp. Todas as outras regras centrais de entrada e risco são preservadas.
