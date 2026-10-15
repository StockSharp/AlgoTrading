# Estratégia Renko Fractals Grid
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Renko Fractals Grid é um port direto do consultor especialista MetaTrader 4 "RENKO FRACTALS GRID". A estratégia opera rompimentos de fractais recentes de Bill Williams confirmados por um filtro de volatilidade estilo Renko, uma tendência de média móvel ponderada e a força do momentum derivada do indicador de taxa de variação. A versão StockSharp mantém o gerenciamento de posições em grade do robô original, incluindo dimensionamento de posições com martingale, gerenciamento de ponto de equilíbrio, trailing stops, proteção de equity e trailing opcional de lucro flutuante em unidades de moeda.

## Lógica de trading
- **Rompimento de fractal:** Uma configuração comprada requer que o fractal de alta mais recente seja rompido pela última vela fechada enquanto pelo menos um dos três fechamentos anteriores permaneceu abaixo desse nível. As operações vendidas espelham esse comportamento com fractais de baixa.
- **Filtro Renko:** A estratégia inspeciona o intervalo de máxima/mínima das últimas _CandlesToRetrace_ barras. Um rompimento é válido somente quando o fechamento atual está pelo menos um "bloco" Renko (seja uma distância fixa em pips ou o último valor ATR) afastado desses extremos.
- **Filtro de tendência:** As médias móveis ponderadas rápida e lenta devem estar alinhadas (rápida acima da lenta para posições compradas e abaixo para vendidas).
- **Verificação de momentum:** O desvio absoluto dos últimos três valores de taxa de variação de 100 deve exceder os limiares configurados. Isso imita o filtro de momentum MQL baseado em `iMomentum`.
- **Confirmação MACD:** As operações são permitidas somente quando a linha principal do MACD está no lado correto de sua linha de sinal. A mesma verificação é usada para o timing de saída.

## Gestão de riscos
- **Grade martingale:** Cada posição adicional multiplica o volume base por _LotExponent_ enquanto o número de operações simultâneas é limitado por _MaxTrades_.
- **Stop-loss e take-profit:** Offsets de preço estáticos em pips são aplicados a partir do preço médio de entrada.
- **Ponto de equilíbrio:** Quando o preço avança por _BreakEvenTriggerPips_, o stop se move para a entrada mais _BreakEvenOffsetPips_.
- **Trailing stop:** Um trailing stop baseado em velas mantém a melhor excursão observada desde a entrada.
- **Trailing monetário:** O gerenciamento opcional de lucro flutuante fecha todas as operações após um recuo de _MoneyStopLoss_ uma vez que o lucro aberto excede _MoneyTakeProfit_.
- **Stop de equity:** A estratégia rastreia o pico de equity em execução (baseado no valor do portfólio e PnL aberto). Se o drawdown exceder _EquityRiskPercent_, toda a posição é liquidada.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `CandleType` | Tipo de vela principal usado para todos os indicadores. |
| `FastMaLength` / `SlowMaLength` | Períodos das médias móveis ponderadas que definem a direção da tendência. |
| `MomentumLength` | Lookback de taxa de variação para o filtro de momentum. |
| `MomentumBuyThreshold` / `MomentumSellThreshold` | Desvio absoluto mínimo de 100 requerido para entradas. |
| `UseAtrFilter` | Usar ATR em vez de uma distância fixa em pips para a confirmação Renko. |
| `BoxSizePips` | Tamanho do bloco Renko sintético quando a filtragem ATR está desabilitada. |
| `CandlesToRetrace` | Número de velas inspecionadas ao medir máximas e mínimas recentes. |
| `BaseVolume` | Volume de operação inicial antes de aplicar o multiplicador martingale. |
| `LotExponent` | Multiplicador aplicado a cada nova posição na grade. |
| `MaxTrades` | Número máximo de posições simultâneas por direção. |
| `StopLossPips` / `TakeProfitPips` | Distâncias de stop protetor estático e objetivo. |
| `TrailingStopPips` | Distância do trailing stop em pips (definir como zero para desabilitar). |
| `UseBreakEven` | Habilitar mover o stop para o ponto de equilíbrio. |
| `BreakEvenTriggerPips` / `BreakEvenOffsetPips` | Distância necessária antes da ativação do ponto de equilíbrio e o offset aplicado depois. |
| `UseMoneyTarget` | Habilitar trailing de lucro flutuante em unidades de moeda. |
| `MoneyTakeProfit` / `MoneyStopLoss` | Limiar de lucro que ativa o trailing monetário e o recuo máximo permitido. |
| `UseEquityStop` | Habilitar stop-out global baseado em equity. |
| `EquityRiskPercent` | Drawdown máximo permitido a partir do pico de equity antes de fechar todas as operações. |

## Notas de implementação
- O EA original avalia o MACD no período mensal. O port do StockSharp usa a mesma configuração de indicadores no período de trabalho porque dados multi-período não estão disponíveis por padrão.
- Todos os offsets de preço que se originaram de "pips" no MQL são convertidos através do passo de preço do instrumento para trabalhar com cotações de pip fracionadas.
- O rastreamento de lucro realizado é aproximado via eventos de ordens preenchidas, o que é suficiente para a lógica de drawdown de equity na ausência de estatísticas de conta fornecidas pelo broker.
- A estratégia utiliza assinaturas de velas de alto nível com vinculação de indicadores conforme exigido pelas diretrizes do projeto e mantém todos os comentários em linha em inglês conforme solicitado.
