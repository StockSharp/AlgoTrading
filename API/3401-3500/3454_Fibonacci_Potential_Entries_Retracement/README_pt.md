# Fibonacci Estratégia de retração de entradas potenciais
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Fibonacci estratégia de retração de entradas potenciais** recria o MetaTrader especialista `EA_PUB_FibonacciPotentialEntries`. O algoritmo espera por cotações de nível 1 ao vivo e, em seguida, coloca duas ordens pendentes em torno dos níveis de retração Fibonacci fornecidos manualmente. Quando a meta de lucro compartilhado é atingida, a estratégia aumenta 50% de cada posição e move o stop de proteção para o ponto de equilíbrio para a quantidade restante.

## Mapeamento da lógica original
- **Ordens de entrada** – Duas ordens de limite são emitidas assim que o melhor preço de compra e o melhor preço de venda estiverem disponíveis:
  - *Primeira ordem*: colocada na retração de 50% (`P50Level`). O stop-loss está ancorado três spreads abaixo (modo de alta) ou acima (modo de baixa) do nível de 61%.
  - *Segunda ordem*: colocada na retração de 61% (`P61Level`) com o stop-loss definido a três spreads do ponto médio entre os níveis de 61% e 100%.
- **Viés de direção** – A entrada `bType` original se torna o parâmetro `MarketBias` (`Bull` para limites de compra, `Bear` para limites de venda).
- **Alocação de risco** – A primeira negociação sempre arrisca `0.7%` do patrimônio do portfólio. A segunda negociação consome a parcela restante de `RiskPercent` (`max(RiskPercent - 0.7, 0)`), mantendo a divisão usada por EA.
- **Cálculo de volume** – O risco é traduzido para o tamanho da posição por meio de `Portfolio.CurrentValue` (com substitutos para `CurrentBalance` e `BeginValue`) junto com a etapa de preço, custo da etapa e multiplicador do instrumento.
- **Take-profit parcial** – Quando o preço ultrapassa `TargetLevel`, cada negociação preenchida envia uma ordem de mercado para fechar metade do seu volume aberto. Posteriormente, a ordem stop é movida para o preço de entrada registrado, correspondendo à sequência `OrderClose` + `OrderModify` de EA.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `P50Level` | Preço atribuído à retração Fibonacci de 50%. |
| `P61Level` | Preço atribuído à retração Fibonacci de 61,8%. |
| `P100Level` | Preço atribuído à retração Fibonacci de 100% (usado para o stop do ponto médio). |
| `TargetLevel` | Meta de lucro compartilhado para ambas as negociações. |
| `RiskPercent` | Orçamento total de risco em percentagem do capital próprio (deve ser ≥ 0,7). |
| `MarketBias` | Escolhe campanha longa (`Bull`) ou curta (`Bear`). |

## Detalhes de execução
1. Assine cotações de nível 1 via `SubscribeLevel1()` e aguarde valores de compra/venda positivos.
2. Calcule spread, níveis de stop e tamanhos de posição. Os pedidos são enviados uma vez por execução e não serão recriados automaticamente posteriormente (mesmo comportamento do especialista MQL).
3. Após o preenchimento, a estratégia registra o preço médio de entrada, coloca a ordem de stop apropriada e rastreia o volume aberto por perna.
4. Quando o mercado imprime além de `TargetLevel`, a estratégia envia uma ordem de mercado de fechamento parcial por perna e subsequentemente move o stop para o ponto de equilíbrio para a quantidade restante.
5. As ordens stop são canceladas quando não resta volume ou quando a estratégia é interrompida.

## Notas e limitações
- O stop loss é regenerado sempre que o tamanho da posição muda. Se a corretora rejeitar ordens stop, verifique as permissões do conector e ajuste as configurações específicas da bolsa de acordo.
- O take-profit não é registrado como ordem pendente. Em vez disso, o algoritmo espelha o EA monitorando o nível de preços e gerenciando as saídas em tempo real.
- Como os pedidos são criados apenas uma vez, reinicie a estratégia para atualizar os pedidos pendentes após a alteração dos parâmetros (idêntico ao fluxo de trabalho MetaTrader).
