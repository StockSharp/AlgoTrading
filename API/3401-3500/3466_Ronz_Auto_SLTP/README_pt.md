# Estratégia Ronz Auto SLTP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Ronz Auto SLTP Strategy** é uma porta C# direta do utilitário MetaTrader 5 *Ronz Auto SLTP*. Ele atua como um gerente comercial que atribui automaticamente níveis protetores de stop-loss e take-profit, aplica bloqueio de lucro e ativa regras de rastreamento para cada posição aberta. A conversão depende do StockSharp API de alto nível e oferece suporte à supervisão em toda a conta e à implantação de símbolo único.

Principais capacidades:

- Aplique proteção do lado do servidor ou virtual (do lado do cliente), dependendo do sinalizador `UseServerStops`.
- Defina distâncias iniciais de stop-loss e take-profit usando medidas de pip no estilo MetaTrader.
- Garanta um valor fixo de lucro depois que a negociação atingir um limite configurável.
- Execute três variações de trailing stop (clássico, distância do passo, passo a passo) espelhando o orientador original.
- Monitore todos os títulos da carteira conectada ou restrinja a gestão apenas ao título da estratégia.
- Emita notificações de log opcionais sempre que um stop virtual ou take-profit fechar uma posição.

## Parâmetros

| Parâmetro | Padrão | Descrição |
| --- | --- | --- |
| `ManageAllSecurities` | `true` | Monitore todas as posições abertas no portfólio. Desative para gerenciar apenas a segurança da estratégia. |
| `TakeProfitPips` | `550` | Distância em MetaTrader pips adicionada ao preço de entrada para a meta de lucro (incluindo a distância mínima de parada do corretor). |
| `StopLossPips` | `350` | Distância em MetaTrader pips subtraída do preço de entrada para o nível de stop loss (incluindo a distância mínima de stop do corretor). |
| `UseServerStops` | `true` | Quando habilitado, envia ordens stop e limit para a corretora. Quando desativado, fecha posições virtualmente assim que os limites são atingidos. |
| `EnableLockProfit` | `true` | Habilite a lógica de bloqueio de lucro que move o stop acima/abaixo do preço de entrada após um limite ser atingido. |
| `LockProfitAfterPips` | `100` | Lucro (em pips) que deve ser alcançado antes que a lógica de bloqueio se torne ativa. Defina como zero para pular o estágio de bloqueio e rastrear imediatamente. |
| `ProfitLockPips` | `60` | Lucro preservado quando o bloqueio for ativado. A parada é movida para o preço de entrada mais/menos esta distância. |
| `TrailingStopMode` | `Classic` | Algoritmo final usado após o limite de bloqueio. Opções: `None`, `Classic`, `StepDistance`, `StepByStep`. |
| `TrailingStopPips` | `50` | Distância final em pips. Atua como o buffer principal para os modos de rastreamento clássico e baseado em etapas. |
| `TrailingStepPips` | `10` | Incremento usado pelos modos de rastreamento baseados em etapas. Ignorado pela variante final clássica. |
| `EnableAlerts` | `false` | Quando verdadeiro, escreve mensagens de log sempre que um stop virtual ou take-profit fecha uma ordem. |

## Detalhes do comportamento

1. **Proteção Inicial**
   - Quando uma nova posição é detectada, a estratégia calcula as metas de stop-loss e take-profit em relação ao preço de entrada.
   - As distâncias mínimas de parada definidas pelo corretor são respeitadas lendo os campos de nível de parada/congelamento das atualizações de Nível 1 e expandindo as distâncias solicitadas, se necessário.

2. ** Bloqueio de lucro **
   - Assim que o lucro atual exceder `LockProfitAfterPips`, o stop é aumentado (ou reduzido para posições vendidas) para bloquear o valor de `ProfitLockPips` de lucro.
   - Se o bloqueio estiver desabilitado, a estratégia pula esta etapa e aguarda as condições finais.

3. **Paradas finais**
   - `Classic`: mantém uma distância fixa (`TrailingStopPips`) em relação ao preço atual.
   - `StepDistance`: reduz a distância em `TrailingStepPips` quando o preço se move favoravelmente o suficiente, correspondendo de perto à implementação MetaTrader "step keep distance".
   - `StepByStep`: empurra o stop para frente em incrementos discretos `TrailingStepPips` assim que o preço avança pela distância final configurada.
   - O rastreamento começa imediatamente quando `LockProfitAfterPips` é zero. Caso contrário, ele será ativado quando o lucro exceder `LockProfitAfterPips + TrailingStopPips`.

4. **Modo Virtual**
   - Quando `UseServerStops` é falso, a estratégia não registra nenhuma ordem stop/limit. Em vez disso, fecha a posição aberta através de ordens de mercado assim que o stop-loss ou o take-profit calculados são violados.
   - Os alertas podem ser ativados para documentar esses fechamentos virtuais no log.

5. **Suporte multi-segurança**
   - Com `ManageAllSecurities = true`, a estratégia assina dados de Nível 1 para cada título que possui uma posição aberta no portfólio selecionado.
   - Cada título mantém seu próprio estado de stop, take-profit e trailing para que as negociações longas e curtas sejam supervisionadas de forma independente.

## Dicas de uso

- Anexe a estratégia a uma carteira e, opcionalmente, atribua um título padrão quando apenas um instrumento precisar de supervisão.
- Certifique-se de que os dados do Nível 1 (melhor oferta/venda) estejam disponíveis para cada símbolo gerenciado para que os cálculos do pip permaneçam precisos.
- Revise as restrições do nível de stop da corretora: a estratégia já expande as distâncias solicitadas, mas configurações extremamente restritas ainda podem ser rejeitadas pela plataforma de negociação.
- O modo virtual é útil em corretoras que não oferecem suporte a ordens de proteção ou durante cenários de backtesting.

## Diferenças do especialista original

- StockSharp agrega posições por título, enquanto o modo de hedge MetaTrader rastreia tickets individuais. O porto, portanto, gerencia a posição líquida por instrumento.
- A funcionalidade de ordem de teste do script MQ5 (abertura de negociações fictícias no testador) foi omitida intencionalmente.
- Os alertas são entregues por meio do subsistema de registro StockSharp, em vez de pop-ups na tela.
