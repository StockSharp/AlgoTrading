# Estratégia Reversing Martingale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Reversing Martingale** é um port direto em C# do expert advisor "Reversing Martingale EA" do MetaTrader. Ela mantém continuamente uma única posição a mercado e alterna a direção da operação após cada negócio fechado. Operações perdedoras disparam uma progressão martingale de volume, enquanto operações lucrativas reiniciam o ciclo para o tamanho de lote inicial. Todas as posições são protegidas por níveis simétricos de stop-loss e take-profit expressos em pontos de preço.

A estratégia não depende de indicadores ou estrutura de mercado. Ela simplesmente reage a posições concluídas e mantém exposição de capital ativa o tempo todo (a menos que a negociação esteja desabilitada).

## Lógica central
1. **Configuração inicial**
   - Quando a estratégia inicia, ela envia imediatamente uma ordem a mercado usando o parâmetro `Start Volume` e o `First Trade Side` configurado.
   - Ordens protetoras de stop-loss e take-profit são anexadas usando a distância especificada em `Target (points)`.
2. **Gestão de posição**
   - Apenas uma posição pode estar aberta por vez. A estratégia aguarda até que a posição atual seja totalmente fechada por suas ordens protetoras ou por ações externas.
   - Após cada saída, a estratégia inverte a direção da operação (compra -> venda ou venda -> compra).
   - Se a última operação realizou perda, o volume da próxima ordem equivale ao tamanho da posição anterior multiplicado por `Lot Multiplier`. Caso contrário, o volume volta para `Start Volume`.
3. **Continuação do ciclo**
   - Depois que novo volume e direção são determinados, a próxima ordem a mercado é enviada imediatamente, mantendo o ciclo martingale alternado em execução.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| **Start Volume** | Volume inicial usado no começo de cada ciclo vencedor. |
| **Lot Multiplier** | Multiplicador de volume aplicado após uma operação perdedora. Deve ser maior que 1. |
| **First Trade Side** | Direção da primeira operação quando a sessão da estratégia começa. |
| **Target (points)** | Distância em passos de preço usada para ordens de stop-loss e take-profit. |
| **Order Comment** | Texto opcional atribuído a cada ordem a mercado gerada. |

## Notas adicionais
- A distância em passos de preço é convertida para `UnitTypes.Step` e passada para `StartProtection`, portanto stop-loss e take-profit ficam sempre ativos.
- Ajustes de volume respeitam passo, mínimo e máximo de volume da security por meio do helper `NormalizeVolume`.
- A estratégia espera eventos de execução do conector; se a negociação for pausada ou o conector ficar offline, o ciclo martingale será retomado assim que a negociação voltar a ser permitida.
