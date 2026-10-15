# Estratégia Vortex Indicator MMRec Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
- Convertido do especialista MetaTrader 5 **Exp_VortexIndicator_MMRec_Duplex.mq5** (MQL ID 23180).
- Mantém dois fluxos independentes do indicador Vortex: um dedicado a operações longas e outro a operações curtas. Cada fluxo tem seu próprio período, comprimento e deslocamento de barra para que a lógica altista e baixista possa ser ajustada separadamente.
- Replica o módulo de recuperação de gestão de dinheiro "MMRec" do EA original. A estratégia rastreia os últimos resultados de operações por direção e muda temporariamente para um tamanho de ordem reduzido após um número configurável de perdas.

## Lógica de Sinal
1. Assinar o tipo de vela configurado para cada fluxo e calcular o indicador Vortex (`VI+` e `VI-`).
2. **Entradas longas:** quando a barra anterior tinha `VI+` abaixo ou igual a `VI-` e a barra atual fecha com `VI+` acima de `VI-` (cruzamento altista). As entradas são permitidas apenas se `AllowLongEntries` estiver habilitado.
3. **Saídas longas:** quando `VI-` sobe acima de `VI+` na barra avaliada, desde que `AllowLongExits` esteja habilitado.
4. **Entradas curtas:** quando a barra anterior tinha `VI+` acima ou igual a `VI-` e a barra atual fecha com `VI+` abaixo de `VI-` (cruzamento baixista), controlado por `AllowShortEntries`.
5. **Saídas curtas:** quando `VI+` sobe de volta acima de `VI-` na barra avaliada, controlado por `AllowShortExits`.
6. Cada direção mantém seus próprios níveis de stop-loss e take-profit medidos em passos de preço. Atingir qualquer um deles fecha imediatamente a posição e registra o resultado para os contadores de recuperação.

## Recuperação de Gestão de Dinheiro
- O EA original inspeciona uma janela deslizante de operações passadas para decidir se a próxima ordem deve usar o volume normal ou o reduzido. Este port replica o mesmo comportamento.
- Para operações longas, a fila armazena até `LongTotalTrigger` resultados de PnL mais recentes. Se pelo menos `LongLossTrigger` deles forem operações perdedoras, a próxima entrada longa usa `LongSmallMoneyManagement`; caso contrário usa `LongMoneyManagement`.
- Operações curtas repetem a mesma lógica com `ShortTotalTrigger`, `ShortLossTrigger`, `ShortSmallMoneyManagement` e `ShortMoneyManagement`.
- Quando os valores de gatilho são zero, as filas são esvaziadas e o volume base é sempre usado.

## Modos de Margem
`MarginModeOption` descreve como o valor de gestão de dinheiro é convertido em um volume executável:
- **FreeMargin (0):** tratar o valor como uma fração do capital (aproximação do modo "margem livre" original).
- **Balance (1):** idêntico a `FreeMargin` neste port; usa o valor atual do portfólio.
- **LossFreeMargin (2):** arriscar uma fração do capital usando a distância de stop-loss configurada. Recorre ao dimensionamento baseado em preço se a distância do stop for zero.
- **LossBalance (3):** igual a `LossFreeMargin` nesta implementação.
- **Lot (4):** interpretar o valor diretamente como volume de ordem.

Todos os tamanhos calculados são normalizados usando o passo de volume do instrumento, bem como as restrições de volume mínimo e máximo.

## Parâmetros
| Parâmetro | Padrão | Descrição |
| --- | --- | --- |
| `LongCandleType` | H4 | Período usado para o indicador Vortex do lado comprado. |
| `ShortCandleType` | H4 | Período usado para o indicador Vortex do lado vendido. |
| `LongLength` | 14 | Período do indicador Vortex para sinais longos. |
| `ShortLength` | 14 | Período do indicador Vortex para sinais curtos. |
| `LongSignalBar` | 1 | Deslocamento de barra fechada avaliado para cruzamentos longos (0 = última barra fechada). |
| `ShortSignalBar` | 1 | Deslocamento de barra fechada avaliado para cruzamentos curtos. |
| `AllowLongEntries` | true | Habilitar entradas longas quando o cruzamento altista aparecer. |
| `AllowLongExits` | true | Habilitar fechamento de posições longas quando `VI-` domina `VI+`. |
| `AllowShortEntries` | true | Habilitar entradas curtas quando o cruzamento baixista aparecer. |
| `AllowShortExits` | true | Habilitar fechamento de posições curtas quando `VI+` domina `VI-`. |
| `LongTotalTrigger` | 5 | Número de operações longas recentes inspecionadas pelo contador de recuperação. |
| `LongLossTrigger` | 3 | Operações longas perdedoras necessárias antes de mudar para o volume longo reduzido. |
| `LongMoneyManagement` | 0.1 | Valor base de gestão de dinheiro para operações longas. |
| `LongSmallMoneyManagement` | 0.01 | Valor reduzido de gestão de dinheiro após uma sequência de perdas longas. |
| `LongMarginMode` | Lot | Interpretação do valor de gestão de dinheiro longo (ver modos acima). |
| `LongStopLossSteps` | 1000 | Distância protetora abaixo da entrada longa expressa em passos de preço. |
| `LongTakeProfitSteps` | 2000 | Distância de take-profit acima da entrada longa expressa em passos de preço. |
| `LongSlippageSteps` | 10 | Tolerância de slippage informativa para ordens longas (não usada para dimensionamento). |
| `ShortTotalTrigger` | 5 | Número de operações curtas recentes inspecionadas pelo contador de recuperação. |
| `ShortLossTrigger` | 3 | Operações curtas perdedoras necessárias antes de mudar para o volume curto reduzido. |
| `ShortMoneyManagement` | 0.1 | Valor base de gestão de dinheiro para operações curtas. |
| `ShortSmallMoneyManagement` | 0.01 | Valor reduzido de gestão de dinheiro após uma sequência de perdas curtas. |
| `ShortMarginMode` | Lot | Interpretação do valor de gestão de dinheiro curto. |
| `ShortStopLossSteps` | 1000 | Distância protetora acima da entrada curta expressa em passos de preço. |
| `ShortTakeProfitSteps` | 2000 | Distância de take-profit abaixo da entrada curta expressa em passos de preço. |
| `ShortSlippageSteps` | 10 | Tolerância de slippage informativa para ordens curtas. |

## Notas de Implementação
- Construído inteiramente na API de alto nível do StockSharp. As assinaturas de velas impulsionam os indicadores Vortex através de `Bind`, que entrega barras finalizadas antes de qualquer decisão ser tomada.
- A lógica de recuperação de operações armazena séries de lucros por direção em filas e replica as funções `BuyTradeMMRecounterS` / `SellTradeMMRecounterS` do MetaTrader.
- Os níveis de stop-loss e take-profit são recalculados em unidades de preço (passo de preço do instrumento × passos configurados) e aplicados em cada vela recebida.
- Os volumes de ordem são normalizados via restrições de `VolumeStep`, `MinVolume` e `MaxVolume` do instrumento para evitar envios inválidos.
- Os parâmetros de slippage são preservados para fins de documentação, mas não são usados diretamente pelos manipuladores de ordens do StockSharp.
