# Estratégia de BB Swing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia BB Swing** é um port fiel do consultor especialista MetaTrader "BB SWING". Opera pullbacks das Bandas de Bollinger alinhados com a tendência predominante definida por duas médias móveis lineares ponderadas (LWMAs). Um filtro de Momentum de período superior e um MACD muito lento ajudam a confirmar a força da reversão antes de qualquer posição ser aberta.

## Lógica de trading

1. Trabalhar apenas com velas concluídas do período de tempo `CandleType`.
2. Rastrear as últimas quatro velas concluídas para inspecionar extremos recentes e corpos de velas.
3. Aguardar que a LWMA rápida permaneça acima (para longos) ou abaixo (para curtos) da LWMA lenta.
4. Verificar que um dos últimos três mínimos toca a banda inferior de Bollinger (configuração longa) ou um dos máximos toca a banda superior (configuração curta).
5. Requerer que a vela anterior tenha um corpo mais forte que sua predecessora, sinalizando Momentum se afastando da banda.
6. Confirmar a força da tendência com o Momentum calculado em `MomentumCandleType`. A estratégia mede a distância absoluta entre a leitura de Momentum e 100; a distância deve exceder os limiares de compra/venda configurados em qualquer um dos últimos três valores de Momentum.
7. Validar a direção de longo prazo com um MACD calculado no período de tempo `MacdCandleType`. Entradas longas são permitidas enquanto a linha principal do MACD permanece acima da linha de sinal; curtos requerem a relação oposta.
8. Quando todas as condições se alinham, entrar em uma posição de mercado usando o passo de volume martingale atual.

## Dimensionamento de posição e escalonamento

- `InitialVolume` define o volume da primeira entrada.
- Cada add-on adicional multiplica o volume base por `LotExponent` (`volume = InitialVolume * LotExponent^n`).
- `MaxTrades` limita o número de add-ons sequenciais para que o tamanho total da posição nunca exceda `InitialVolume * MaxTrades`.

## Regras de saída e proteção

- Valores fixos de `StopLoss` e `TakeProfit` expressos em passos de preço.
- Lógica opcional de ponto de equilíbrio (`EnableBreakEven`) que move o stop para `BreakEvenOffset` quando o preço avança `BreakEvenTrigger` passos.
- Trailing stop clássico (`EnableTrailingStop`) que segue o preço extremo por `TrailingStop` passos.
- Ferramentas de gestão de capital:
  - `UseMoneyTakeProfit` fecha posições quando o lucro não realizado em moeda de conta atinge `MoneyTakeProfit`.
  - `UsePercentTakeProfit` fecha posições quando o lucro equivale a `PercentTakeProfit` por cento do capital inicial.
  - `UseMoneyTrailing` ativa um trail de lucro: quando o lucro excede `MoneyTrailTarget`, um recuo de `MoneyTrailStop` aciona uma saída.
- `UseEquityStop` monitora o drawdown de capital relativo ao pico de capital registrado durante a sessão. Um drawdown maior que `EquityRiskPercent` fecha todas as posições.
- `CloseOnMacdCross` opcional sai sempre que a linha principal do MACD cruzar a linha de sinal contra a direção da posição atual.

Todas as ações de proteção dependem de ordens de mercado (`BuyMarket` / `SellMarket`) para neutralizar toda a posição.

## Parâmetros

| Nome | Descrição |
|------|-------------|
| `InitialVolume` | Volume base de negociação usado para a primeira entrada. |
| `LotExponent` | Multiplicador aplicado ao volume de cada entrada adicional ao escalonar. |
| `MaxTrades` | Número máximo de add-ons sequenciais permitidos a qualquer momento. |
| `TakeProfit` | Take profit expresso em passos de preço. |
| `StopLoss` | Stop loss expresso em passos de preço. |
| `FastMaPeriod` | Período da LWMA rápida calculada em preços típicos. |
| `SlowMaPeriod` | Período da LWMA lenta calculada em preços típicos. |
| `MomentumLength` | Número de barras usadas no cálculo do Momentum. |
| `MomentumBuyThreshold` | Distância mínima de 100 para que o Momentum do período superior valide operações longas. |
| `MomentumSellThreshold` | Distância mínima de 100 para que o Momentum do período superior valide operações curtas. |
| `EnableBreakEven` | Habilita o movimento de stop para ponto de equilíbrio. |
| `BreakEvenTrigger` | Passos de preço necessários para acionar o movimento de ponto de equilíbrio. |
| `BreakEvenOffset` | Offset aplicado ao stop após ativação do ponto de equilíbrio. |
| `EnableTrailingStop` | Habilita o trailing stop clássico em passos de preço. |
| `TrailingStop` | Tamanho do trailing stop expresso em passos. |
| `UseMoneyTakeProfit` | Habilita tomada de lucro fixa em moeda de conta. |
| `MoneyTakeProfit` | Lucro em moeda que fecha a posição quando `UseMoneyTakeProfit` está ativo. |
| `UsePercentTakeProfit` | Habilita tomada de lucro baseada em percentual de capital. |
| `PercentTakeProfit` | Percentual do capital inicial que aciona uma saída quando `UsePercentTakeProfit` está ativo. |
| `UseMoneyTrailing` | Habilita trailing baseado em capital após atingir um lucro alvo. |
| `MoneyTrailTarget` | Nível de lucro que ativa a lógica de trailing monetário. |
| `MoneyTrailStop` | Recuo máximo permitido em moeda após ativação. |
| `UseEquityStop` | Habilita o fechamento de posições quando o drawdown flutuante excede um limiar. |
| `EquityRiskPercent` | Drawdown máximo de capital permitido em percentual. |
| `CloseOnMacdCross` | Habilita filtragem de saída baseada em MACD. |
| `CandleType` | Período de tempo primário usado para cálculos de sinais. |
| `MomentumCandleType` | Período superior usado para o filtro de Momentum. |
| `MacdCandleType` | Período muito lento usado pelo filtro de saída MACD. |

## Notas

- A estratégia processa apenas velas concluídas; não reage dentro de uma barra.
- Todos os cálculos de stop e alvo usam o passo de preço do instrumento reportado pelo exchange conectado. Certifique-se de que `PriceStep` esteja corretamente configurado para controle preciso de risco.
- Proteções monetárias e baseadas em capital dependem das estatísticas de portfólio de estratégia disponíveis no StockSharp. Ao executar no modo tester, certifique-se de que o feed do portfólio esteja habilitado.
- Ao contrário do especialista MQL original, esta implementação em C# mantém uma única posição agregada por direção. O escalonamento aumenta a posição agregada em vez de abrir múltiplos tickets discretos.
- As Bandas de Bollinger usam comprimento fixo de 20 e largura de 2 desvios padrão em preços típicos, correspondendo ao código original.
