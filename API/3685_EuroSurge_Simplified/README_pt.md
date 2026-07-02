# Estratégia simplificada EuroSurge
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Converte o consultor especialista MetaTrader 4 **"EuroSurge Simplified"** em StockSharp API de alto nível.
- Negocia em velas finalizadas e avalia uma coleção de indicadores clássicos (MA, RSI, MACD, Bollinger Bandas, Stochastic) para encontrar entradas.
- Aplica um período de resfriamento configurável entre negociações e atribui níveis de take-profit/stop-loss expressos em etapas de preço.
- Suporta vários modos de dimensionamento de posição: volume fixo, porcentagem de saldo e porcentagem de patrimônio.

## Sinais
1. **Tendência da média móvel** (opcional): um SMA rápido de 20 períodos deve estar acima (longo) ou abaixo (curto) de um SMA configurável mais lento.
2. **Filtro RSI** (opcional): RSI deve ficar abaixo do limite longo para permitir compras e acima do limite curto para permitir vendas.
3. **MACD confirmação** (opcional): a linha MACD deve ser maior que (longa) ou menor que (curta) a linha de sinal.
4. **Bollinger Filtro de bandas** (opcional): o preço deve ultrapassar a banda inferior para posições compradas ou a faixa superior para posições vendidas.
5. **Stochastic filtro** (opcional): %K e %D precisam permanecer abaixo de 50 para posições compradas ou acima de 50 para posições vendidas.

Todos os filtros habilitados devem concordar antes que a estratégia envie uma ordem de mercado. A exposição oposta é achatada antes da abertura de uma nova posição, refletindo a lógica MetaTrader de substituição de negociações abertas.

## Gestão de risco
- As distâncias de stop-loss e take-profit são definidas em etapas de preço (MetaTrader “pontos”).
- A estratégia registra automaticamente ordens de proteção com `SetStopLoss` e `SetTakeProfit` logo após a abertura de uma posição.
- As negociações são bloqueadas até que o intervalo configurado em minutos tenha decorrido desde a última ordem preenchida.

## Dimensionamento de posições
- **FixedSize**: negocia com o `FixedVolume` configurado.
- **BalancePercent**: aloca uma fração do saldo inicial do portfólio e aproxima o volume dividindo pelo último preço de fechamento.
- **EquityPercent**: comporta-se da mesma forma, mas depende do patrimônio atual do portfólio.
- Os volumes são ajustados à etapa de volume de segurança e fixados entre os limites mínimo/máximo da exchange.

## Parâmetros
| Nome | Descrição |
| ---- | ----------- |
| `TradeSizeType` | Modo de dimensionamento de posição (fixo, % de saldo, % de patrimônio).
| `FixedVolume` | Volume usado quando `TradeSizeType = FixedSize`.
| `TradeSizePercent` | Porcentagem aplicada em dimensionamento baseado em porcentagem.
| `TakeProfitPoints` / `StopLossPoints` | Distâncias de proteção nas etapas de preços.
| `MinTradeIntervalMinutes` | Esfriamento entre negociações.
| `MaPeriod` | Comprimento SMA lento (SMA rápido é fixado em 20 em linha com EA).
| `RsiPeriod`, `RsiBuyLevel`, `RsiSellLevel` | RSI configuração e limites.
| `MacdFast`, `MacdSlow`, `MacdSignal` | Parâmetros MACD.
| `BollingerLength`, `BollingerWidth` | Bollinger Configurações de bandas.
| `StochasticLength`, `StochasticK`, `StochasticD` | Stochastic parâmetros do oscilador.
| `UseMa`, `UseRsi`, `UseMacd`, `UseBollinger`, `UseStochastic` | Alternar filtros individuais.
| `CandleType` | Prazo usado para avaliação do sinal.

## MetaTrader Diferenças
- O EA original valida o volume em relação às restrições específicas do corretor. A porta reflete isso ajustando-se a StockSharp etapas de volume e respeitando o volume mínimo/máximo quando disponível.
- Os níveis de proteção são convertidos em etapas de preço por meio de ajudantes StockSharp em vez da aritmética manual de preços.
- Todos os valores do indicador são consumidos por meio da ligação de alto nível API sem chamadas diretas para `GetValue`.

## Dicas de uso
1. Anexe a estratégia a um portfólio e um título e configure o prazo por meio de `CandleType`.
2. Ajuste as alternâncias do indicador para reproduzir ou simplificar o comportamento original EA.
3. Aumente `MinTradeIntervalMinutes` se precisar de menos negociações; diminua-o para entradas mais frequentes.
4. Verifique se `TakeProfitPoints` e `StopLossPoints` correspondem ao tamanho do tick do símbolo.
