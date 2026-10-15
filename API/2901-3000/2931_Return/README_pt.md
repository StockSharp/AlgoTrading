# Estratégia de Retorno
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o clássico consultor especialista "Return Strategy". Ela prepara uma grade de ordens de compra limitada e venda limitada em pares no início de uma janela de trading configurada. A grade é simétrica em torno do preço de mercado, usa espaçamento fixo em pips e pode ser dimensionada por um volume fixo ou um modelo de risco percentual. Uma vez que as ordens são executadas, a estratégia supervisiona a posição com lógica de stop-loss estática e de trailing, monitora o lucro aberto acumulado e força um fechamento completo no horário de corte diário ou toda sexta-feira.

O sistema original foi projetado para contas de netting e focado na captura de movimentos de reversão à média após horários programados. A conversão mantém essa estrutura enquanto adapta o gerenciamento de ordens, trailing e controles de capital à API de alto nível do StockSharp.

## Regras de Trading

- **Preparação diária** – No `StartHour` a estratégia verifica que nenhuma ordem de grade está ativa e coloca `PendingOrderCount` limites de compra abaixo e limites de venda acima do preço atual. O primeiro nível é deslocado por `DistancePips` e cada nível subsequente adiciona `StepPips` de espaçamento.
- **Controle de risco** – Cada ordem pendente pode usar um `OrderVolume` fixo ou um tamanho baseado em risco derivado de `RiskPercent`. Quando o dimensionamento por risco é usado, o capital disponível e a distância do stop-loss determinam o volume por ordem para que o risco total da grade iguale a porcentagem configurada.
- **Gestão de stops** – Cada posição executada recebe um stop-loss inicial baseado em `StopLossPips`. Se `TrailingStopPips` for maior que zero, uma vez que o preço avança além do limite de trailing, o stop é ajustado em etapas de `TrailingStepPips`.
- **Alvo de lucro e saída de sessão** – O lucro aberto líquido é rastreado em pips. Quando ele atinge `TotalProfitPips` a estratégia marca todas as posições e ordens para fechamento. Também realiza o mesmo esvaziamento no `EndHour` configurado e toda sexta-feira independentemente do lucro.
- **Expiração de ordens** – Ordens pendentes podem expirar automaticamente após `ExpirationHours`. Ordens expiradas ou canceladas manualmente são removidas da lista de rastreamento para permitir que uma nova grade seja colocada no dia seguinte.

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| `StopLossPips` | Distância do stop inicial para qualquer posição executada (em pips ajustados). |
| `StartHour` | Hora (0–23) quando a grade de ordens pendentes é criada. |
| `EndHour` | Hora (0–23) que desencadeia uma saída completa de posições e ordens. |
| `TotalProfitPips` | Alvo de lucro aberto líquido (em pips) que força o fechamento de todos os trades. |
| `TrailingStopPips` | Distância do trailing stop a partir do preço após ativação. Definir como zero para desabilitar o trailing. |
| `TrailingStepPips` | Avanço adicional necessário antes de mover o trailing stop. Deve ser positivo quando o trailing estiver habilitado. |
| `DistancePips` | Deslocamento inicial para a primeira ordem pendente em cada lado do mercado. |
| `StepPips` | Espaçamento incremental entre ordens pendentes consecutivas. |
| `PendingOrderCount` | Número de limites de compra e limites de venda a registrar no `StartHour`. |
| `ExpirationHours` | Vida útil de ordens pendentes em horas. Zero desabilita a expiração. |
| `OrderVolume` | Volume fixo por ordem pendente. Deixar em zero para habilitar o dimensionamento baseado em risco. |
| `RiskPercent` | Porcentagem do portfólio alocada para toda a grade. O tamanho por ordem é derivado desse valor quando `OrderVolume` é zero. |
| `CandleType` | Série de velas usada para controlar a lógica de timing e gestão de stops. |

## Notas Adicionais

- A conversão de pips espelha a lógica original do MetaTrader ajustando o tamanho do passo para instrumentos de três e cinco decimais.
- Quando `RiskPercent` é usado, a porcentagem se aplica à grade combinada e é dividida igualmente entre todas as ordens pendentes.
- A estratégia aplica regras de validação idênticas ao EA fonte: as horas devem estar dentro do intervalo diário, o trailing requer um passo diferente de zero, e apenas um de `OrderVolume`/`RiskPercent` pode estar ativo de cada vez.
- Todos os comentários públicos no código são fornecidos em inglês por consistência com as diretrizes do repositório.
