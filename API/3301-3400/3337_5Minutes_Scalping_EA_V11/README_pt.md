# FiveMinutesScalpingEA v1.1 (porta StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **FiveMinutesScalpingEaV11Strategy** é uma conversão do consultor especialista MetaTrader 4 *5MinutesScalpingEA v1.1*. A estratégia mantém o conceito original de combinar médias móveis de Hull de vários períodos, uma transformada de Fisher de momento, um detector de fuga ATR e um filtro de tendência para escalpelar movimentos de curta duração em um gráfico de cinco minutos. A implementação segue o StockSharp alto nível API e usa assinaturas de velas com ligações de indicadores para reproduzir o comportamento do consultor especialista.

A estratégia foi projetada para negociação de símbolo único. Apenas uma posição líquida é mantida a qualquer momento e todos os sinais são avaliados em velas concluídas. As ordens de proteção são simuladas dentro da estratégia monitorando os máximos e mínimos das velas.

## Pilha de indicadores
| Componente | StockSharp implementação | Objetivo |
|-----------|--------------------------|---------|
| `i1` casco MA personalizado | `HullMovingAverage` com ponto `Period1` (padrão 30) | Detecta a direção da tendência rápida através da inclinação da média móvel de Hull. |
| `i2` casco MA personalizado | `HullMovingAverage` com ponto `Period2` (padrão 50) | Confirma a direção mais ampla da tendência; ambos os filtros Hull devem concordar para entradas no modo normal. |
| `i3` Impulso de Fisher | `FisherTransform` with period `Period3` | Atua como um oscilador de momento. Valores positivos favorecem setups longos, valores negativos favorecem setups curtos. |
| `i4` ATR setas de ruptura | `AverageTrueRange` com período `Period4` combinado com comparações de velas | Procura rompimentos fortes onde a máxima/mínima atual excede as duas máximas/mínimas anteriores em pelo menos um ATR. |
| `i5` Filtro de tendência Fisher | `FisherTransform` with period `Period5` | Fornece uma confirmação de tendência suavizada semelhante ao histograma de tendência original EA. |

Para cada indicador, a estratégia armazena valores históricos para que possa ler o valor `IndicatorShift` velas de volta, correspondendo ao parâmetro MQL4 `IndicatorsShift`. Todos os filtros podem ser desabilitados individualmente através de seus respectivos parâmetros.

## Lógica de negociação
1. A estratégia segue a série de velas definida por `CandleType` (padrão: velas de 5 minutos).
2. A cada vela finalizada, os indicadores Hull, Fisher e ATR são atualizados. Quando há histórico suficiente disponível, a estratégia avalia a vela que está `IndicatorShift` barras atrás.
3. **Modo normal** (`SignalMode = Normal`):
   - Uma entrada **longa** requer que todos os filtros habilitados relatem condições de alta (inclinação positiva de Hull, momentum de Fisher acima de zero, ATR rompimento para cima, tendência de Fisher acima de zero).
   - Uma entrada **curta** requer que todos os filtros habilitados relatem condições de baixa (inclinação negativa de Hull, impulso de Fisher abaixo de zero, ATR rompimento para baixo, tendência de Fisher abaixo de zero).
4. **Modo reverso** (`SignalMode = Reverse`) simplesmente troca a interpretação das condições de alta e baixa.
5. Um novo sinal inverte o sinalizador interno `_lastSignal`. Se `CloseOnSignal` estiver ativado, a posição oposta será fechada imediatamente antes de uma nova entrada ser enviada.
6. O parâmetro `UseTimeFilter` restringe entradas ao intervalo `[StartHour, EndHour)` (com comportamento envolvente idêntico ao MQL4 EA).

## Gestão de risco
A porta StockSharp implementa os seguintes recursos de proteção:
- **Stop loss / takeprofit** – Se ativado, os preços stop e alvo são colocados a uma distância fixa (`StopLossPips`, `TakeProfitPips`) do preço de entrada e monitorados em cada vela.
- **Trailing stop** – Quando `UseTrailingStop` está ativado, uma âncora final é mantida. Assim que o preço avança `TrailingStepPips`, o stop é movido para que permaneça `TrailingStopPips` longe do extremo atual.
- **Ponto de equilíbrio** – Se `UseBreakEven` estiver ativado e o preço se mover em `BreakEvenPips + BreakEvenAfterPips`, o stop será reduzido a `BreakEvenPips` de distância da entrada.
- **Posição única** – Todas as saídas são executadas através de ordens de mercado (`SellMarket` / `BuyMarket`) que fecham toda a posição líquida.

## Parâmetros
| Nome | Padrão | Descrição |
|------|---------|-------------|
| `CandleType` | M5 | Prazo primário. |
| `IndicatorShift` | 1 | Número de velas fechadas para analisar ao avaliar os filtros. |
| `SignalMode` | Normais | Use sinais normais ou invertidos. |
| `UseIndicator1`..`UseIndicator5` | verdade | Alterna cada filtro. |
| `Period1`, `Period2`, `Period3`, `Period4`, `Period5` | 30, 50, 10, 14, 18 | Períodos para cálculos de Hull, Fisher e ATR. |
| `PriceMode3` | Alto Baixo | Parâmetro de compatibilidade para a seleção de preço original da Fisher. A implementação StockSharp sempre alimenta o preço padrão da vela para o indicador Fisher. |
| `CloseOnSignal` | falso | Feche a posição oposta quando aparecer um novo sinal de entrada. |
| `UseTimeFilter`, `StartHour`, `EndHour` | falso, 0, 0 | Janela de negociação intradiária opcional. |
| `UseTakeProfit`, `TakeProfitPips` | verdade, 10 | Faça o gerenciamento do lucro. |
| `UseStopLoss`, `StopLossPips` | verdade, 10 | Pare o gerenciamento de perdas. |
| `UseTrailingStop`, `TrailingStopPips`, `TrailingStepPips` | falso, 1, 1 | Gerenciamento de trailing stop. |
| `UseBreakEven`, `BreakEvenPips`, `BreakEvenAfterPips` | falso, 4, 2 | Lógica de parada de equilíbrio. |
| `TradeVolume` | 0,01 | Volume para entradas no mercado. |

## Diferenças vs. original EA
- A lógica de fechamento da cesta (`UseBasketClose`, `CloseInProfit`, `CloseInLoss`) não é implementada porque a estratégia StockSharp funciona com uma única posição líquida.
- Dimensionamento automático de lote (`AutoLotSize` / `RiskFactor`) e verificações de spread não fazem parte desta porta. Use o ambiente de hospedagem para controlar o volume e o deslizamento.
- O parâmetro do modo de preço Fisher é exposto para compatibilidade, mas o StockSharp `FisherTransform` atualmente usa o preço padrão da vela. Outros modos de preço podem ser emulados estendendo o indicador, se necessário.
- O gerenciamento comercial é realizado em velas concluídas, o que reflete o comportamento EA quando `IndicatorsShift >= 1`.

## Dicas de uso
1. Anexe a estratégia a um instrumento líquido com spreads reduzidos (o EA foi originalmente projetado para EUR/USD M5).
2. Configure `TradeVolume` de acordo com as regras de dimensionamento de sua conta.
3. Ajuste os períodos dos indicadores ou desative os filtros para corresponder à sua tolerância ao risco.
4. Combine com o filtro de tempo integrado para evitar sessões de baixa liquidez.
5. Sempre valide as configurações no testador StockSharp antes de executar em dados ativos.
