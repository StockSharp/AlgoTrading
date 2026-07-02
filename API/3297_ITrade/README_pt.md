# Estratégia iTrade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é um gestor manual de vendas convertido do expert advisor do MetaTrader **iTrade**. Ela recria o fluxo de botões no gráfico do EA original: sempre que o usuário solicita uma venda, uma posição martingale é aberta. A estratégia então observa o lucro flutuante de todas as operações vendidas e liquida os tickets mais e menos lucrativos quando alvos de lucro predefinidos são atingidos.

## Lógica central

- Ordens são abertas apenas por solicitações explícitas do usuário. Chame `QueueSellRequest()` para simular o pressionamento do botão no MetaTrader.
- A primeira posição usa o **volume inicial** configurado. Após cada operação perdedora, o tamanho da próxima ordem é multiplicado pelo **multiplicador martingale**. Operações lucrativas redefinem a sequência para o volume base.
- Lucro flutuante é medido usando o melhor preço ask atual. Quando o lucro médio por operação aberta atinge o **alvo de lucro médio**, a estratégia fecha as operações mais e menos lucrativas do lote ativo (até **contagem base de operações**).
- Se mais de **contagem base de operações** posições estiverem abertas, o **alvo de lucro estendido** mais rigoroso é aplicado antes de fechar duas operações.
- Cálculos de lucro dependem dos valores `PriceStep` e `StepPrice` do ativo. A estratégia lança uma exceção durante a inicialização quando eles estão ausentes.

## Parâmetros

| Nome | Descrição |
| ---- | --------- |
| `InitialVolume` | Tamanho de lote base usado para a primeira ordem martingale. |
| `MartingaleMultiplier` | Multiplicador aplicado após cada operação perdedora. |
| `AverageProfitTarget` | Lucro flutuante médio (em moeda) exigido para fechar operações dentro do primeiro lote. |
| `ExtendedAverageProfitTarget` | Limite de lucro flutuante médio quando mais que o lote base está ativo. |
| `BaseTradeCount` | Número de operações consideradas parte do lote inicial. |
| `ControlInterval` | Frequência das verificações internas (intervalo do temporizador). |

## Notas de uso

1. Defina `Security`, `Portfolio` e quaisquer parâmetros desejados antes de iniciar a estratégia.
2. Chame `QueueSellRequest()` sempre que uma nova venda deve ser aberta. A estratégia dimensionará a ordem segundo as regras martingale e enviará uma venda a mercado.
3. O algoritmo armazena um histórico de resultados de operações fechadas (até 200 entradas) para reproduzir o comportamento martingale original.
4. Ordens de fechamento são enviadas como compras a mercado pelo volume exato das operações alvo.

## Diferenças em relação à versão MetaTrader

- A versão MetaTrader dependia de botões no gráfico; aqui o usuário aciona vendas programaticamente via `QueueSellRequest()`.
- A execução de ordens é tratada por ordens a mercado do StockSharp. Execuções parciais são agregadas automaticamente pela estratégia.
- Limites de lucro operam sobre valores decimais de moeda usando `StepPrice`, enquanto o EA original usava funções de lucro de tickets do MetaTrader.
