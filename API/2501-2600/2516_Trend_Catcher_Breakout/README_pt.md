# Estratégia de Rompimento Trend Catcher
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
A estratégia Trend Catcher é uma conversão do assessor especialista do MetaTrader 5 "Trend_Catcher_v2". Combina três médias móveis exponenciais com o indicador Parabolic SAR para identificar reversões de tendência e oportunidades de continuação de tendência. O sistema opera em um único símbolo e período e depende de cálculos no final da vela, tornando-o adequado tanto para backtesting no StockSharp Designer quanto para execução ao vivo através de executores baseados na API do StockSharp.

## Indicadores e Filtros
- **Parabolic SAR** — detecta reversões de alta e de baixa que indicam possíveis reversões.
- **EMA lenta** — o filtro de tendência de período superior que define a direção dominante.
- **EMA rápida** — reage mais rapidamente às mudanças de preço para confirmar a direção do swing atual.
- **EMA de disparo** — mantém a entrada próxima à ação do preço e evita negociações realizadas muito longe da média.
- **Interruptores de dias de trading** — filtros opcionais para desabilitar o trading em dias da semana selecionados.

## Lógica de Trading
### Entradas compradas
1. O preço de fechamento termina acima do valor atual do Parabolic SAR.
2. A vela anterior fechou abaixo do valor anterior do Parabolic SAR (reversão de alta).
3. A EMA rápida está acima da EMA lenta, confirmando uma tendência de alta.
4. O preço de fechamento está acima da EMA de disparo para evitar sinais contra a tendência.
5. Nenhuma posição está aberta e nenhuma posição foi fechada durante a vela atual.

### Entradas vendidas
Todas as condições acima são espelhadas:
1. O preço de fechamento termina abaixo do valor atual do Parabolic SAR.
2. A vela anterior fechou acima do valor anterior do Parabolic SAR (reversão de baixa).
3. A EMA rápida está abaixo da EMA lenta.
4. O preço de fechamento está abaixo da EMA de disparo.
5. Nenhuma posição está aberta e nenhuma posição foi fechada durante a vela atual.

Quando o interruptor **Reverse Signals** está habilitado, as condições compradas e vendidas são invertidas, permitindo que a estratégia opere rompimentos na direção oposta.

## Gestão de Posições
- **Stop-loss automático** – quando habilitado, o stop é calculado a partir da distância entre o preço e o Parabolic SAR multiplicada pelo `StopLossCoefficient`. A distância é limitada entre `MinStopLoss` e `MaxStopLoss`.
- **Take profit automático** – multiplica a distância do stop por `TakeProfitCoefficient`. Distâncias manuais podem ser usadas quando a automação está desabilitada.
- **Dimensionamento de posição baseado em risco** – o tamanho da negociação é derivado do patrimônio do portfólio e `RiskPercent`. Quando a negociação fechada mais recente é uma perda e **Use Martingale** está habilitado, o tamanho calculado é multiplicado por `MartingaleMultiplier`.
- **Breakeven e trailing stop** – após atingir o lucro `BreakevenTrigger`, o stop é movido para o preço de entrada mais `BreakevenOffset` (ou menos para negociações vendidas). Uma vez que a posição ganha `TrailingTrigger`, o stop segue o preço por `TrailingStep`.
- **Fechar em sinal oposto** – quando ativo, a estratégia sai de uma posição existente assim que uma configuração oposta aparecer.
- **Uma negociação por vela** – o algoritmo armazena o timestamp da última saída e pula entradas até a próxima vela abrir.

## Parâmetros
| Nome | Descrição | Valor padrão |
| --- | --- | --- |
| `CandleType` | Período principal usado para todos os indicadores. | Período de 15 minutos |
| `CloseOnOppositeSignal` | Sair imediatamente quando a configuração inversa é detectada. | `true` |
| `ReverseSignals` | Trocar condições compradas e vendidas. | `false` |
| `TradeMonday` … `TradeFriday` | Habilitar ou desabilitar o trading em dias da semana específicos. | `true` |
| `SlowMaPeriod` | Período do filtro de tendência EMA lenta. | `200` |
| `FastMaPeriod` | Período da confirmação EMA rápida. | `50` |
| `FastFilterPeriod` | Período da EMA de disparo. | `25` |
| `SarStep` | Passo de aceleração do Parabolic SAR. | `0.004` |
| `SarMax` | Aceleração máxima do Parabolic SAR. | `0.2` |
| `AutoStopLoss` | Habilitar cálculo dinâmico do stop-loss. | `true` |
| `AutoTakeProfit` | Habilitar cálculo dinâmico do take profit. | `true` |
| `MinStopLoss` / `MaxStopLoss` | Limites inferior e superior para a distância do stop. | `0.001` / `0.2` |
| `StopLossCoefficient` | Multiplicador aplicado à distância SAR. | `1` |
| `TakeProfitCoefficient` | Multiplicador usado para a distância do take profit. | `1` |
| `ManualStopLoss` | Distância de stop fixa quando a automação está desabilitada. | `0.002` |
| `ManualTakeProfit` | Distância de alvo fixa quando a automação está desabilitada. | `0.02` |
| `RiskPercent` | Porcentagem do patrimônio do portfólio arriscado por negociação. | `2` |
| `UseMartingale` | Aumentar o tamanho após uma negociação perdedora. | `true` |
| `MartingaleMultiplier` | Multiplicador aplicado após uma perda. | `2` |
| `BreakevenTrigger` | Lucro necessário antes de mover o stop para o ponto de equilíbrio. | `0.005` |
| `BreakevenOffset` | Buffer adicionado quando o stop é movido para o ponto de equilíbrio. | `0.0001` |
| `TrailingTrigger` | Lucro necessário para começar a seguir o stop. | `0.005` |
| `TrailingStep` | Distância mantida pelo trailing stop. | `0.001` |

## Notas de uso
- A estratégia envia ordens de mercado tanto para entradas quanto para saídas; controles de slippage devem ser adicionados ao nível do adaptador de corretagem se necessário.
- Como a lógica usa dados de fim de vela, a precisão dos backtests depende da granularidade da série de velas fornecida à estratégia.
- Os parâmetros são totalmente expostos através de objetos `StrategyParam`, tornando-os disponíveis para otimização no StockSharp Designer.
