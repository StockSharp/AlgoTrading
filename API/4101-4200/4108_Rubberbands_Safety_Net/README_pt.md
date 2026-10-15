# Estratégia de rede de segurança de elásticos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

StockSharp porta do consultor especialista RUBBERBANDS 1.6 MetaTrader. O sistema original mantém um par protegido de bilhetes de compra e venda, reinjeta o lado fechado após cada lucro e ativa uma rede de segurança quando a perda corrente excede os limites de caixa predefinidos. A conversão mantém o ciclo alternado, mas adapta a mecânica ao modelo de posição líquida de StockSharp calculando a média na direção da exposição atual em vez de manter ordens de hedge independentes.

## Lógica de negociação

- **Início do ciclo:** No início de cada minuto, ou quando `Enter Now` é alternado, a estratégia abre uma posição de mercado usando `BaseVolume`. O próximo ciclo alterna a direção (comprar, depois vender, depois comprar novamente, etc.).
- **Meta de lucro base:** o lucro líquido não realizado em execução é comparado com `TargetProfitPerLot * BaseVolume`. Quando alcançada, a posição é liquidada e o próximo ciclo muda de direção.
- **Controle de sessão:** `UseSessionTakeProfit` e `UseSessionStopLoss` observam o lucro acumulado realizado mais o lucro não realizado medido em dinheiro por lote base. Atingir qualquer um dos limites aciona uma liquidação completa e zera os contadores.
- **Modo de segurança:** Quando ativado e a perda não realizada exceder `SafetyStartPerLot * BaseVolume`, o algoritmo entra no modo de segurança e começa a calcular a média na direção atual enviando pedidos adicionais de tamanho `SafetyVolume`. Cada perda extra de `SafetyStepPerLot` por lote de segurança programa outra ordem média.
- **Saídas de segurança:** Enquanto estiver no modo de segurança, a posição é achatada quando o ganho não realizado atinge `SafetyProfitPerLot * |Position|` ou quando a métrica do nível da sessão ultrapassa `SafetyModeTakeProfitPerLot * BaseVolume`.

## Condições de Entrada

### Entradas longas
- Nenhuma exposição aberta e o minuto acabou de passar ou `Enter Now` é verdade.
- A estratégia atualmente espera abrir uma posição longa (ciclos alternados).
- O interruptor de parada manual está desativado.

### Entradas curtas
- Igual às condições longas, mas a direção do próximo ciclo é curta.

## Gerenciamento de saída

- **Alvo base atingido:** Feche toda a posição e inverta a direção do ciclo.
- **Sessão TP/SL:** Feche a posição, limpe os contadores de lucro realizado e permaneça estável até o gatilho do próximo minuto.
- **Lucro de segurança:** Feche a posição quando a meta de PnL líquido for atingida enquanto o modo de segurança estiver ativo.
- **Média de segurança:** Ordens de segurança adicionais são anexadas quando a perda não realizada cresce em incrementos de `SafetyStepPerLot`.
- **Fechamento manual:** A configuração `Close Now` fecha a posição na próxima vela e zera o acumulador de lucro realizado.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `BaseVolume` | Tamanho da ordem de mercado para a etapa primária. |
| `TargetProfitPerLot` | Meta de lucro (dinheiro por lote) para o comércio base. |
| `UseSessionTakeProfit` / `SessionTakeProfitPerLot` | Habilite e configure o lucro em toda a sessão. |
| `UseSessionStopLoss` / `SessionStopLossPerLot` | Habilite e configure o stop loss em toda a sessão. |
| `UseSafetyMode` | Alterne a lógica de média de segurança. |
| `SafetyStartPerLot` | Perda por lote base que ativa o modo de segurança. |
| `SafetyVolume` | Volume de cada ordem de média de segurança. |
| `SafetyStepPerLot` | Perda adicional por lote de segurança necessária para enfileirar outra ordem de segurança. |
| `SafetyProfitPerLot` | Meta de lucro aplicada no modo de segurança. |
| `SafetyModeTakeProfitPerLot` | Meta de lucro no nível da sessão enquanto o modo de segurança está ativo. |
| `UseInitialState`, `InitialProfitSoFar`, `InitialSafetyMode`, `InitialSafetyToBuy`, `InitialUsedSafetyCount` | Ajudantes de restauração de estado para reinicializações. |
| `QuiesceNow`, `Enter Now`, `Stop Trading`, `Close Now` | Chaves manuais que espelham as variáveis externas EA originais. |
| `CandleType` | Período de tempo da série de velas que aciona o loop (padrão 1 minuto). |

## Notas práticas

- StockSharp mantém uma única posição líquida por instrumento. Em vez de manter bilhetes de compra e venda simultâneos, a conversão é calculada para a posição existente quando o modo de segurança está ativo. Isto preserva os limites baseados em dinheiro, ao mesmo tempo que está em conformidade com o modelo de compensação.
- Os limites de lucros e perdas são expressos na moeda da conta por lote, refletindo as entradas externas MetaTrader. Ajuste-os para corresponder ao valor do tick do instrumento.
- As opções manuais (`Stop Trading`, `Close Now`, `Enter Now`, `Quiesce`) podem ser alteradas instantaneamente na interface do usuário para controlar a estratégia sem editar o código.
- `StartProtection()` é invocado no início para reutilizar a estrutura de proteção StockSharp padrão para controles de risco.
- Certifique-se de que os metadados do instrumento (`VolumeStep`, `VolumeMin`, `VolumeMax`) estejam configurados para que os volumes solicitados passem na validação de troca; o auxiliar os alinha automaticamente com a etapa válida mais próxima.
