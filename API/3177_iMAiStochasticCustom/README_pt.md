# Estratégia de iMA iStochastic Custom
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia replica o expert do MetaTrader **"iMA iStochastic Custom"** dentro do framework StockSharp. Combina um envelope de média móvel com um filtro de oscilador estocástico. O trading ocorre nas velas concluídas do período selecionado (`CandleType`). Todos os comentários abaixo usam a mesma nomenclatura do advisor original.

Componentes principais:

1. **Envelope de média móvel** – a média móvel base é deslocada por `LevelUpPips` e `LevelDownPips` (expressos em pips) para construir bandas de resistência e suporte. O método de média corresponde às opções do MetaTrader: Simples, Exponencial, Suavizada (SMMA) e Ponderada Linear (LWMA).
2. **Oscilador estocástico** – os comprimentos de %K, %D e suavização seguem os parâmetros originais. Dois limites (`StochasticLevel1` e `StochasticLevel2`) validam condições de sobrecompra/sobrevenda.
3. **Gestão monetária** – o seletor original de `lot`/`risk` é preservado através do parâmetro `ManagementMode`. No modo `FixedLot`, o tamanho da ordem equivale a `VolumeValue`. No modo `RiskPercent`, a estratégia arrisca o percentual configurado do patrimônio do portfólio contra a distância do stop-loss, reproduzindo o comportamento de `CMoneyFixedMargin`.
4. **Proteções** – as distâncias de stop-loss, take-profit e trailing são inseridas em pips. O trailing é atualizado em velas concluídas, espelhando a lógica MQL enquanto permanece compatível com o modelo de eventos do StockSharp.

## Lógica de trading
Os sinais de compra e venda são simétricos:

- **Compra** quando o fechamento da vela está acima do envelope superior (`ma + LevelUpPips`) e qualquer linha do estocástico está acima de `StochasticLevel1`.
- **Venda** quando o fechamento da vela está abaixo do envelope inferior (`ma + LevelDownPips`) e qualquer linha do estocástico está abaixo de `StochasticLevel2`.
- Definir `ReverseSignals = true` troca a direção de entrada.

Apenas uma posição líquida está ativa por vez. Quando o sinal muda, a estratégia envia uma ordem grande o suficiente para neutralizar a exposição atual e abrir uma nova posição na direção oposta.

## Controle de risco e saídas
- **Stop-loss / take-profit** – distâncias em pips convertidas através do `PriceStep` do instrumento. São verificadas em cada vela finalizada usando a máxima/mínima da vela.
- **Trailing stop** – começa depois que o preço se moveu `TrailingStopPips` a favor da posição. Requer uma melhoria adicional de `TrailingStepPips` antes de cada ajuste, assim como a rotina de trailing MQL.
- **Gestão monetária** – no modo de risco o tamanho da posição é `equity * VolumeValue / 100 / perUnitRisk`, onde `perUnitRisk` é a perda monetária por um lote até o stop-loss.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-----------|
| `CandleType` | Período usado para cálculos. |
| `StopLossPips`, `TakeProfitPips` | Distâncias protetoras em pips. |
| `TrailingStopPips`, `TrailingStepPips` | Ativação do trailing e passo (pips). Definir zero para desabilitar. |
| `ManagementMode`, `VolumeValue` | Dimensionamento de lote fixo ou percentual de risco. |
| `MaPeriod`, `MaShift`, `MaMethod` | Comprimento da média móvel, deslocamento de barras e método (SMA/EMA/SMMA/LWMA). |
| `LevelUpPips`, `LevelDownPips` | Deslocamentos do envelope superior/inferior em pips. Valores negativos são permitidos para a banda inferior. |
| `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlowing` | Configuração do oscilador. |
| `StochasticLevel1`, `StochasticLevel2` | Níveis de confirmação para verificações de compra/venda. |
| `ReverseSignals` | Inverter a direção de todas as operações. |

## Notas de implementação
- Velas, indicadores e ordens são conectados através da API de alto nível (`SubscribeCandles().BindEx(...)`).
- O tamanho do pip se ajusta automaticamente a símbolos forex de 3/5 dígitos multiplicando o `PriceStep` quando necessário.
- A lógica de trailing é executada em velas concluídas. Se o trailing intrabarra for necessário, conectar a lógica a dados de nível tick.
- Nenhum port Python é fornecido; a pasta `PY` está intencionalmente ausente conforme solicitado.

## Diferenças em relação à versão do MetaTrader
- O dimensionamento de risco é explícito e baseado nas métricas do portfólio StockSharp em vez da classe auxiliar `CMoneyFixedMargin`. Os lotes resultantes correspondem ao comportamento original quando o stop-loss está habilitado; com stop-loss zero o tamanho da posição permanece zero, espelhando a proteção MQL.
- As verificações de proteção (stop-loss, take-profit, trailing) são avaliadas em velas concluídas porque as estratégias StockSharp são orientadas a eventos. Isso mantém a lógica determinística e corresponde às restrições de backtesting.
- O logging é simplificado para a saída padrão do StockSharp; o flag verboso `InpPrintLog` não é transferido.

Use esta estratégia como substituto direto ao migrar do MetaTrader para o StockSharp Designer ou Runner. Ajuste os parâmetros para o instrumento e período alvo.
