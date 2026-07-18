# Estratégia Momo Trades V3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Momo Trades V3 é uma estratégia de impulso convertida do consultor especialista MetaTrader original. Ele combina um detector de padrão MACD multicondição com um filtro de média móvel exponencial deslocada (EMA). A porta StockSharp mantém os elementos discricionários do EA, adiciona manipulação opcional de ponto de equilíbrio e fornece um modo de dimensionamento de posição baseado em risco que reflete a lógica de lote automática do script.

## Lógica de negociação
1. **MACD padrões de momentum** – a estratégia observa a linha MACD principal usando os parâmetros clássicos `(12, 26, 9)` e um deslocamento adicional (`MacdShift`). Dois padrões de alta são aceitos:
   - Uma sequência estritamente crescente em que o terceiro valor é igual a zero e as duas amostras subsequentes continuam a aumentar.
   - Uma sequência em que MACD cruza acima de zero, com as amostras seguintes permanecendo positivas enquanto os valores anteriores são negativos.
As entradas de baixa requerem condições espelhadas com valores decrescentes e a linha cruzando abaixo de zero.
2. **EMA filtro de distância** – o preço de fechamento da barra deslocada (`MaShift`) deve ser pelo menos `PriceShiftPoints` MetaTrader pontos acima de EMA para negociações longas e abaixo de EMA para vendas curtas. Isso evita entradas quando o preço está próximo da média.
3. **Regime de posição única** – a estratégia abre uma nova posição somente quando esta é plana. Os sinais opostos são ignorados enquanto uma negociação está ativa.
4. **Saída de fechamento da sessão** – quando `CloseEndDay` está habilitado, a estratégia liquida qualquer posição às 23h no horário da plataforma (21h às sextas-feiras) para evitar exposição durante a noite.
5. **Parada de equilíbrio opcional** – quando `UseBreakeven` está ativado, uma vez que o preço se move o suficiente para colocar um stop no preço de entrada mais `BreakevenOffsetPoints`, a estratégia arma um nível de equilíbrio. Se o preço retornar para ou além desse nível, a posição será fechada no mercado.

## Gestão de risco
- **Proteção inicial** – `StopLossPoints` e `TakeProfitPoints` são convertidos em distâncias de preço absoluto por meio da etapa de preço do instrumento e passados para `StartProtection`, portanto, as ordens de proteção são anexadas automaticamente.
- **Volume automático** – se `UseAutoVolume` for verdadeiro, o tamanho do pedido será calculado a partir do patrimônio atual do portfólio. A estratégia aloca `RiskFraction` de patrimônio para a negociação, divide pelo valor do contrato (`price × lot size`), normaliza o resultado para a etapa de volume de troca e respeita os limites `VolumeMin`/`VolumeMax`. Quando o dimensionamento automático está desativado, `TradeVolume` é usado diretamente.

## Indicadores
- **Moving Average Convergence Divergence (MACD)** – fornece o sinal de impulso principal e é avaliado em amostras históricas usando `MacdShift`.
- **Média móvel exponencial (EMA)** – usada como filtro de tendência deslocada.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
|------|------|---------|-------------|
| `CandleType` | `DataType` | `TimeFrame(15m)` | Período primário usado para geração de sinal. |
| `MaPeriod` | `int` | `22` | Período EMA para o filtro de deslocamento. |
| `MaShift` | `int` | `1` | Número de barras concluídas usadas na amostragem do preço de fechamento e EMA. |
| `FastPeriod` | `int` | `12` | Comprimento EMA rápido para MACD. |
| `SlowPeriod` | `int` | `26` | Comprimento EMA lento para MACD. |
| `SignalPeriod` | `int` | `9` | Comprimento do sinal EMA para MACD. |
| `MacdShift` | `int` | `1` | Deslocamento adicional aplicado ao avaliar os padrões MACD. |
| `PriceShiftPoints` | `decimal` | `10` | Distância mínima (em MetaTrader pontos) entre o fechamento deslocado e o EMA necessário para abrir uma posição. |
| `TradeVolume` | `decimal` | `0.1` | Volume de negociação padrão quando o dimensionamento automático está desativado. |
| `RiskFraction` | `decimal` | `0.1` | Fração do patrimônio do portfólio usada para dimensionar o pedido quando `UseAutoVolume` for verdadeiro. |
| `UseAutoVolume` | `bool` | `false` | Permite dimensionamento de volume baseado em risco. |
| `StopLossPoints` | `decimal` | `100` | Distância inicial de stop-loss expressa em MetaTrader pontos. `0` desativa a parada protetora. |
| `TakeProfitPoints` | `decimal` | `0` | Distância inicial de lucro em MetaTrader pontos. `0` desativa o alvo. |
| `CloseEndDay` | `bool` | `true` | Fecha posições abertas próximo ao final do dia de negociação (23h ou 21h às sextas-feiras). |
| `UseBreakeven` | `bool` | `false` | Ativa a lógica de gerenciamento do ponto de equilíbrio. |
| `BreakevenOffsetPoints` | `decimal` | `0` | Compensação adicionada ao preço de entrada ao armar a saída do ponto de equilíbrio. |

## Notas de uso
- Certifique-se de que o instrumento tenha um `PriceStep` válido; caso contrário, a estratégia volta para um valor de `0.0001` pontos ao converter MetaTrader pontos em distâncias de preço.
- O filtro MACD depende de velas finalizadas; a estratégia sai mais cedo para barras inacabadas para corresponder ao comportamento original EA.
- Como apenas uma posição é permitida por vez, o risco por negociação permanece controlado pelo único `TradeVolume` (ou pelo equivalente de tamanho automático).
