# Estratégia KSRobot 1.5
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia KSRobot 1.5** é uma conversão C# do MetaTrader 4 consultor especialista `KSRobot_1_5_h1_v1.mq4`. A versão StockSharp mantém a ideia original de negociar quebras de preço Kijun-sen confirmadas por uma média móvel ponderada linear de 20 períodos (LWMA), ao mesmo tempo que impõe uma janela de negociação rigorosa e controles de risco em camadas. Todos os cálculos são realizados em velas de 30 minutos por padrão, mas o prazo pode ser alterado através de um parâmetro.

## Dados e indicadores de mercado
- **Ichimoku** indicador com períodos Tenkan/Kijun/Senkou Span B 12/06/24 por padrão.
- **Média Móvel Ponderada Linear (LWMA)** com comprimento 20 para medir a inclinação e filtro de distância mínima.
- **Velas com prazo** definidas por `CandleType` (o padrão é M30) para geração de sinal.

## Lógica de negociação
### Fluxo de trabalho longo
1. Uma vela deve interagir com a linha Kijun por baixo. Qualquer uma das seguintes opções é suficiente: a vela abre abaixo e fecha acima, o fechamento anterior estava abaixo enquanto o novo fechamento está acima, ou a mínima da vela perfura o nível.
2. O último valor de Kijun é estável ou superior a duas barras atrás, evitando negociações contra um movimento descendente imediato da linha de base.
3. O LWMA está pelo menos `MaFilterPips` (convertido em unidades de preço) abaixo de Kijun. Isso reproduz o requisito de que a média móvel fique abaixo da linha de base em alguns pips.
4. A inclinação do LWMA é positiva (LWMA atual maior que a barra anterior).
5. A configuração é armazenada como um longo pendente até que a condição de inclinação seja satisfeita; apenas um lado pode estar pendente em um determinado momento, imitando os sinalizadores `longcross`/`shortcross` de MQL.
6. Quando todos os critérios estão alinhados e não existe exposição longa líquida, uma ordem de compra de mercado é enviada. O preço de entrada armazenado em cache pela estratégia torna-se a base para o gerenciamento de stop, break-even e trailing.

### Fluxo de trabalho curto
Aplicam-se condições de espelho:
1. A vela interage com Kijun de cima (abre acima e fecha abaixo, fechamento anterior acima e fechamento atual abaixo, ou a máxima toca o nível).
2. Kijun é plano ou inferior a duas barras atrás.
3. O LWMA fica `MaFilterPips` acima de Kijun.
4. A inclinação do LWMA é negativa em comparação com a barra anterior.
5. Apenas uma posição curta pendente é rastreada e é eliminada quando um sinal longo aparece, assim como o especialista original.
6. Quando estiver satisfeito e a conta ainda não estiver curta, uma ordem de venda a mercado é enviada.

## Regras de saída e controle de risco
- **Janela de tempo** – novas negociações são consideradas apenas enquanto o horário de abertura da vela estiver dentro de `[TradingStartHour, TradingEndHour)`, horário padrão das 07h00 às 19h00.
- **Stop-loss inicial** – definido `StopLossPips` abaixo/acima do preço de entrada (convertido através do tamanho do pip do instrumento). Se for zero, nenhuma parada inicial será rastreada.
- **Movimento de ponto de equilíbrio** – assim que o lucro não realizado exceder `BreakEvenPips`, o stop é movido para o preço de entrada mais um pip para posições compradas (menos um para posições vendidas). Este comportamento é controlado por `_breakEvenStep` para emular a lógica MT4 “mover para BE+1”.
- **Trailing Stop** – quando o preço avança `TrailingStopPips`, o stop segue nessa distância apenas na direção favorável.
- **Take-profit** – distância alvo fixa opcional definida por `TakeProfitPips`. Defina como zero para desativar.
- **Slope exit** – se o LWMA virar contra a negociação antes que o stop cruze a entrada, a posição é fechada imediatamente. Isso captura a saída "MA errada" do script MQL.
- **Prioridade** – quando o stop-loss e o take-profit seriam tocados dentro da mesma vela, o stop-loss tem precedência para permanecer conservador com os dados da vela.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `TenkanPeriod` | 6 | Comprimento Tenkan-sen do indicador Ichimoku. |
| `KijunPeriod` | 12 | Comprimento Kijun-sen (gatilho principal). |
| `SenkouSpanBPeriod` | 24 | Comprimento Senkou Span B. |
| `LwmaPeriod` | 20 | Período da confirmação LWMA. |
| `MaFilterPips` | 6 | Distância mínima do pip entre LWMA e Kijun. |
| `StopLossPips` | 50 | Distância inicial de parada protetora. |
| `BreakEvenPips` | 9 | Lucro necessário antes de mover o stop para o ponto de equilíbrio. |
| `TrailingStopPips` | 10 | Distância do trailing stop após o preço passar para o lucro. |
| `TakeProfitPips` | 120 | Distância fixa opcional de lucro. |
| `TradingStartHour` | 7 | Hora inclusiva para começar a processar novas negociações. |
| `TradingEndHour` | 19 | Hora exclusiva para interromper novas entradas. |
| `CandleType` | Prazo de 30 minutos | Tipo de dados usado para assinatura de velas. |

Todos os parâmetros baseados em pip são convertidos em unidades de preço usando `Security.PriceStep` (ou `MinPriceStep`). Os instrumentos cotados com três ou cinco dígitos decimais recebem um multiplicador automático de ×10 para recriar o tamanho padrão do pip FX.

## Notas de implementação
- A estratégia vincula indicadores Ichimoku e LWMA por meio de `SubscribeCandles().BindEx(...)`, garantindo que os valores venham diretamente do pipeline do indicador sem coletas manuais.
- O gerenciamento de posições reflete o especialista MT4: os níveis pendentes substituem os sinalizadores `longcross`/`shortcross` e são limpos assim que uma negociação é acionada.
- Os níveis de proteção são armazenados em cache após a entrada para que as decisões de ponto de equilíbrio e de acompanhamento funcionem com dados em nível de vela, mesmo sem atualizações de pedidos individuais.
- `StartProtection` é invocado com distância zero porque todas as ações de proteção são tratadas dentro do código de estratégia, correspondendo à lógica MT4 personalizada.
- Somente ordens de mercado são usadas. A seleção original de limite versus mercado baseava-se em ticks de compra/venda que não estão disponíveis em backtests baseados em velas.

## Uso
1. Crie a instância de estratégia, atribua `Security`, `Portfolio`, `Volume` e inicie-a dentro do ambiente StockSharp.
2. Opcionalmente, ajuste os parâmetros baseados em pip para o instrumento específico. As predefinições otimizadas dos comentários MQL (GBPUSD, EURUSD) podem ser reproduzidas alterando os padrões antes da execução.
3. Fique de olho na saída do registro: entradas, movimentos de ponto de equilíbrio, ajustes posteriores e saídas de emergência são relatados por meio de chamadas `LogInfo`.
4. Anexe a área do gráfico gerado (velas, Ichimoku, LWMA, negociações próprias) no designer ou backtester para visualizar o fluxo de negociação.

Somente a versão C# é fornecida. Nenhuma pasta Python é criada de acordo com os requisitos.
