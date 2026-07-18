# Estratégia Martingale Bone Crusher
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia Martingale Bone Crusher** replica o comportamento do expert advisor original do MetaTrader. A estratégia opera na direção de uma comparação de médias móveis rápida/lenta e aplica um modelo de gestão de dinheiro martingale que aumenta o tamanho da ordem após uma operação perdedora. Um grande conjunto de ferramentas de gestão de risco está disponível, incluindo alvos de dinheiro fixo, alvos de porcentagem, um movimento de breakeven configurável, níveis clássicos de stop-loss/take-profit medidos em passos de preço, e um trailing stop de proteção de lucro medido em dinheiro.

## Lógica de trading

- **Geração de sinais** – duas médias móveis simples são calculadas na série de candles principal. Quando a média rápida está abaixo da lenta, a estratégia busca entradas compradas. Quando está acima, busca entradas vendidas. Novos trades não são realizados enquanto há uma posição ativa.
- **Sequenciamento martingale** – após cada trade concluído, o tamanho da próxima posição é atualizado. Se o último trade fechou com perda, o próximo volume é multiplicado ou incrementado (dependendo das configurações). Trades vencedores redefiniam o tamanho da posição ao valor inicial.
- **Seleção de modo** – duas variantes de martingale são fornecidas:
  - `Martingale1`: o próximo trade sempre segue a direção atual da média móvel, mesmo após uma perda.
  - `Martingale2`: após uma perda, o próximo trade é revertido em relação à direção perdedora. Isso espelha o comportamento da segunda opção do Expert Advisor original.
- **Controles de risco** – enquanto uma posição está aberta, a estratégia avalia continuamente:
  - níveis clássicos de stop-loss e take-profit expressos em passos de preço;
  - um trailing stop opcional que segue o preço extremo com uma distância de passo fixa;
  - um movimento de breakeven que desloca o nível de saída depois que a posição se move a favor em uma distância configurável;
  - alvos globais de lucro baseados em dinheiro e porcentagem que fecham a posição quando o PnL flutuante agregado excede os limites;
  - um trailing stop adicional em dinheiro que assegura o lucro acumulado assim que o ganho flutuante atinge o nível de ativação.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `UseTakeProfitMoney` | Habilita um alvo de take-profit de dinheiro fixo. |
| `TakeProfitMoney` | Quantidade de dinheiro que fecha o trade quando `UseTakeProfitMoney` está ativo. |
| `UseTakeProfitPercent` | Habilita um alvo de lucro expresso como porcentagem do valor inicial do portfólio. |
| `TakeProfitPercent` | Porcentagem usada quando `UseTakeProfitPercent` está habilitado. |
| `EnableTrailing` | Habilita o trailing stop baseado em dinheiro. |
| `TrailingTakeProfitMoney` | Lucro flutuante necessário para armar o trailing stop de dinheiro. |
| `TrailingStopMoney` | Queda permitida a partir do pico de lucro flutuante após o trailing stop estar ativo. |
| `MartingaleModes` | Seleciona entre o comportamento `Martingale1` e `Martingale2`. |
| `UseMoveToBreakeven` | Habilita o ajuste de stop de breakeven. |
| `MoveToBreakevenTrigger` | Passos de preço que o trade deve se mover a favor antes de a proteção de breakeven ser ativada. |
| `BreakevenOffset` | Distância adicionada ao preço de entrada quando o stop de breakeven é colocado. |
| `Multiply` | Multiplicador aplicado ao próximo volume após uma perda quando `DoubleLotSize` é `true`. |
| `InitialVolume` | Volume de ordem base usado para o primeiro trade e após ganhos. |
| `DoubleLotSize` | Alterna entre dimensionamento martingale multiplicativo (`true`) e aditivo (`false`). |
| `LotSizeIncrement` | Incremento de volume aplicado após uma perda quando `DoubleLotSize` é `false`. |
| `TrailingStopSteps` | Distância do trailing stop em passos de preço. |
| `StopLossSteps` | Distância clássica de stop-loss em passos de preço. |
| `TakeProfitSteps` | Distância clássica de take-profit em passos de preço. |
| `FastPeriod` | Período da média móvel simples rápida. |
| `SlowPeriod` | Período da média móvel simples lenta. |
| `CandleType` | Série de candles usada para todos os cálculos de indicadores. |

## Notas

- O volume de posição é alinhado com o passo de volume do instrumento, os limites mínimos e máximos.
- Os cálculos de lucro flutuante dependem do `PriceStep` e `StepPrice` do instrumento. Se forem zero, as proteções baseadas em dinheiro são automaticamente ignoradas.
- Apenas a implementação em C# é fornecida. O port em Python é omitido intencionalmente conforme os requisitos da tarefa.
