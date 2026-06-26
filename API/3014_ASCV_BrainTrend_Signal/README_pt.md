# Estratégia de Sinal ASCV BrainTrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia de Sinal ASCV BrainTrend** é uma conversão do especialista do MetaTrader que opera sobre sinais do indicador BrainTrend1. A versão do StockSharp se baseia em vinculações de indicadores de alto nível para combinar o Average True Range (ATR), o Oscilador Estocástico e a Jurik Moving Average (JMA) com o objetivo de detectar reversões de impulso e colocar operações com stops de proteção opcionais.

## Ideia Central

1. Calcular o ATR para medir a volatilidade atual e definir uma banda de confirmação dinâmica.
2. Suavizar os preços de fechamento com uma Jurik Moving Average e comparar o valor atual com o valor de duas barras atrás.
3. Quando a diferença suavizada é maior que `ATR / 2.3`, atualizar o estado da lógica BrainTrend:
   - `%K` do Oscilador Estocástico abaixo de **47** alterna o sistema para uma possível configuração vendida.
   - `%K` acima de **53** alterna o sistema para uma possível configuração comprada.
4. Um sinal da barra anterior é executado na próxima vela completada. Os sinais podem ser invertidos com o parâmetro **Reverse Signals**.
5. Os níveis de stop-loss, take-profit e trailing-stop são definidos em pips (múltiplos do passo de preço do instrumento).

## Regras de Entrada e Saída

- **Entrada comprada**: A barra anterior emitiu um sinal de compra e a estratégia não está já comprada. O tamanho da ordem equivale a `Volume + abs(posição atual)`, de modo que os vendidos são cobertos antes de abrir o novo comprado.
- **Entrada vendida**: A barra anterior emitiu um sinal de venda e a estratégia não está já vendida.
- **Stop-loss**: Colocado em `preço de entrada ± StopLossPips * passo de preço`. Se o preço ultrapassar o nível de stop dentro da próxima vela, a posição é fechada a mercado.
- **Take-profit**: Take profit opcional em `preço de entrada ± TakeProfitPips * passo de preço`.
- **Trailing-stop**: Ativado quando tanto `TrailingStopPips` quanto `TrailingStepPips` são maiores que zero. Depois que o preço se move `TrailingStopPips + TrailingStepPips` a favor da operação, o stop é arrastado atrás do movimento por `TrailingStopPips`.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `AtrPeriod` | Período de média do ATR para estimativa de volatilidade. | 14 |
| `StochasticPeriod` | Período base para o Oscilador Estocástico. | 12 |
| `JmaLength` | Comprimento de suavização da Jurik Moving Average. | 7 |
| `StopLossPips` | Distância de stop-loss em pips (passos de preço). | 15 |
| `TakeProfitPips` | Distância de take-profit em pips. | 46 |
| `TrailingStopPips` | Distância de trailing stop em pips. | 0 (desabilitado) |
| `TrailingStepPips` | Movimento favorável mínimo necessário antes do trailing. | 5 |
| `ReverseSignals` | Inverter sinais de compra/venda. | false |
| `CandleType` | Período de trabalho, padrão velas de 15 minutos. | 15m |

## Notas

- Todos os cálculos de indicadores são realizados em velas terminadas para evitar ruído no meio da barra.
- Se o instrumento não fornecer `MinPriceStep`, um passo padrão de `0.0001` é usado ao converter distâncias de pips.
- A estratégia desenha velas, o oscilador estocástico e o JMA no gráfico para monitoramento.
- Os trailing stops reproduzem a lógica original do MetaTrader: eles só se movem na direção da operação e exigem que os limiares de distância e passo sejam cumpridos.

## Dicas de Uso

- Ajustar `AtrPeriod` e `StochasticPeriod` para se adequar à volatilidade do instrumento operado.
- Aumentar os parâmetros de risco baseados em pips ao operar ativos com tamanhos de tick maiores (p. ex., futuros) para evitar saídas imediatas.
- Habilitar `ReverseSignals` para replicar o modo inverso do Consultor Especialista original.
- Combinar com controles de risco do corretor se o trading com dinheiro real estiver envolvido.
