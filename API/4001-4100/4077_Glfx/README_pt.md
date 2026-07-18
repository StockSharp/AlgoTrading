# Estratégia GLFX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

MetaTrader 4 consultor especialista **GLFX** reescrito para StockSharp de alto nível API. A porta preserva a ideia original de combinar confirmações de prazos mais elevados com portões rígidos de gestão de dinheiro, ao mesmo tempo que remove a enorme coleção de filtros raramente usados ​​que dependiam de indicadores externos.

## Lógica de negociação

1. A estratégia funciona em um período primário (padrão **M15**) e, opcionalmente, cria um período de confirmação subindo a escada clássica MetaTrader (`M15 → M30 → H1 → H4 → D1 → W1 → MN`).
2. Um período de tempo maior **RSI** (período padrão 57) rastreia se o momentum está aumentando ou diminuindo. Uma confirmação de compra aparece quando RSI sobe, mas permanece abaixo do teto de sobrecompra configurado. Uma confirmação de venda exige que RSI diminua enquanto permanece acima do piso de sobrevenda.
3. Uma **média móvel simples** de período de tempo mais alto (período padrão 60) detecta se o preço está se afastando da média. Uma confirmação de alta precisa que o MA suba enquanto permanece acima do fechamento atual (preço voltando para uma tendência de alta). Uma confirmação de baixa reflete essa lógica.
4. Cada filtro ativado contribui com `+1` para sentimento de alta ou `-1` para sentimento de baixa. O total deve atingir o número de filtros ativos para contar como um sinal válido. Os contadores lembram quantos sinais consecutivos de força total apareceram (`SignalsRepeat`). Se a força combinada cair abaixo do limite e `SignalsReset` estiver ativado, os contadores serão reiniciados.
5. Quando a estratégia é plana e as opções de entrada longa/curta permitem isso, o próximo contador concluído aciona uma ordem de mercado com o `Volume` configurado. Os níveis estáticos de stop-loss e take-profit são convertidos de pips em compensações de preço usando o tamanho do tick do instrumento.
6. Se uma posição já estiver aberta, fortes sinais opostos podem fechá-la antecipadamente (`AllowLongExit` / `AllowShortExit`). Caso contrário, as saídas dependem da parada ou destino gerenciado por `StartProtection()`.

A porta **não** reproduz o Quantum opcional do EA original, sentimento do Twitter, correlação de barra aberta, teste de conjunto ou escadas avançadas de gerenciamento de dinheiro. Esses módulos exigiam indicadores personalizados adicionais ou estado do corretor que não existem em StockSharp.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `CandleType` | M15 | Prazo de trabalho para avaliação de preços. |
| `HigherTimeFrameShift` | 1 | Número de etapas MT4 usadas para construir o prazo de confirmação. `0` mantém o prazo atual. |
| `UseRsiSignal` | verdade | Ative a confirmação de prazo superior RSI. |
| `RsiPeriod` | 57 | Período da confirmação RSI. |
| `RsiUpperThreshold` | 65 | Desative novos longos quando RSI exceder esse valor. |
| `RsiLowerThreshold` | 25 | Desative novos shorts quando RSI ficar abaixo desse valor. |
| `UseMaSignal` | verdade | Habilite a confirmação da média móvel em prazos mais altos. |
| `MaPeriod` | 60 | Período da média móvel de confirmação. |
| `SignalsRepeat` | 1 | Número de sinais consecutivos de força total necessários antes de abrir uma negociação. |
| `SignalsReset` | verdade | Redefina os contadores quando o sinal combinado perder impulso. |
| `TakeProfitPips` | 308 | Distância de lucro expressa em pips. Defina como `0` para desativar. |
| `StopLossPips` | 290 | Distância de stop-loss expressa em pips. Defina como `0` para desativar. |
| `Volume` | 0,1 | Tamanho do pedido utilizado para novas posições (lotes). |
| `AllowLongEntry` / `AllowShortEntry` | verdade | Chaves de permissão para abertura de negociações longas ou curtas. |
| `AllowLongExit` / `AllowShortExit` | verdade | Permitir o fechamento automático da exposição existente em sinais opostos. |

## Notas de uso

- Escolha instrumentos com um tamanho de tick confiável para que a conversão do pip permaneça precisa. Os pares Forex com três ou cinco casas decimais são mapeados automaticamente para MetaTrader "pontos" multiplicando a etapa de preço por dez.
- Defina `HigherTimeFrameShift` como `0` se quiser executar tudo no mesmo período. Neste caso, os indicadores são alimentados pelo fluxo de velas primário para evitar assinaturas duplicadas.
- Se você precisar do comportamento legado de manter as negociações abertas independentemente dos sinais opostos, desative o sinalizador `Allow*Exit` correspondente.
- O escalonamento de gerenciamento de dinheiro (configurações `MMC_*`), módulos finais e filtros de saída exóticos do script original foram omitidos intencionalmente. Implemente-os sobre este núcleo limpo, se necessário.

## Diferenças do original EA

| Grupo de recursos | MetaTrader EA | StockSharp porta |
|---------------|---------------|-----------------|
| Filtros de confirmação | RSI, MA, Quantum opcional, TSI, correlação multimoeda | RSI e MA apenas (comportamento principal) |
| Controle de entrada | Repetição de sinal mais filtros temporais | Repetição de sinal mais reinicialização opcional |
| Controle de risco | TP/SL estático com grande biblioteca de módulos finais | TP/SL estático via `StartProtection()` |
| Gestão de dinheiro | Escala incremental de lote e escadas de perda | Parâmetro de volume fixo |
| Dependências externas | Indicadores personalizados (`Quantum`, `TSI`, carregamento de conjunto baseado em arquivo) | Nenhum |

O resultado é uma estratégia compacta e sustentável que mantém o comportamento reconhecível do GLFX – aguardando a confirmação da tendência em um gráfico mais lento e entrando somente após acordo repetido – ao mesmo tempo que é fácil de estender usando a estrutura StockSharp.
