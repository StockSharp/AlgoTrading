# Estratégia de impulso e-TurboFx
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **e-TurboFx Momentum Strategy** é uma versão direta do MetaTrader consultor especialista original "e-TurboFx". O sistema verifica as velas finalizadas mais recentes e procura trechos direcionais onde os corpos das velas continuam se expandindo. Velas de baixa consecutivas com tamanho de corpo crescente sinalizam uma capitulação potencial que pode ser atenuada com uma entrada longa, enquanto velas de alta consecutivas com corpos em expansão sugerem uma alta prolongada que pode ser vendida a descoberto. A implementação StockSharp mantém a lógica orientada a eventos por meio de assinaturas de velas e anexa automaticamente proteção opcional de stop-loss e take-profit.

## Lógica de negociação
1. Assine um tipo de vela configurável (período de tempo) e processe apenas velas finalizadas.
2. Acompanhe duas sequências separadas: uma para velas de baixa e outra para velas de alta.
3. Para cada vela, meça o tamanho absoluto do corpo (`|Close - Open|`).
4. Redefina a sequência de direção oposta assim que uma vela fechar na outra direção.
5. Dentro de cada sequência são necessários corpos estritamente em expansão – cada nova vela deve ter um corpo maior que a anterior. Qualquer contração reinicia o contador de sequência a partir de 1.
6. Quando o número de velas em uma sequência atingir `DepthAnalysis`, acione uma entrada no mercado na direção oposta da última sequência (compre após sequências de baixa, venda após sequências de alta).
7. Assim que uma posição estiver aberta, pause a detecção do sinal até que a estratégia retorne a uma posição plana. O `StartProtection` integrado gerencia distâncias opcionais de stop-loss e take-profit expressas em etapas de preço (ticks).

Este comportamento reproduz o algoritmo MQL4 onde o consultor especialista verificou as últimas `N` velas fechadas e confirmou que todos os corpos estavam alinhados na mesma direção e que cada corpo era maior que o corpo da próxima vela mais antiga.

## Detalhes de implementação
- Usa a assinatura de velas de alto nível API com `SubscribeCandles` e `Bind` para permanecer em conformidade com as diretrizes do projeto.
- Mantém apenas campos escalares (`_bearishSequence`, `_bullishSequence`, `_previousBearishBody`, `_previousBullishBody`) para evitar coleções personalizadas e confiar no estado interno entre eventos.
- Chama `StartProtection` apenas uma vez em `OnStarted` para configurar ordens opcionais de stop-loss e take-profit em etapas de preço. Um valor de `0` desativa cada ordem de proteção, assim como o especialista original.
- Fornece extensos comentários em inglês no código-fonte, incluindo explicações para redefinições e gatilhos de entrada.
- Desenha velas e negociações próprias em uma área do gráfico ao executar dentro do Designer ou na interface do usuário para facilitar a depuração.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `DepthAnalysis` | Número de velas concluídas consecutivas necessárias em uma direção com corpos em expansão antes de abrir uma negociação. | `3` |
| `TakeProfitSteps` | Distância de take-profit medida em etapas de preços de câmbio (ticks). Defina como `0` para desativar o lucro. | `120` |
| `StopLossSteps` | Distância de stop-loss medida em etapas de preços de câmbio (ticks). Defina como `0` para desativar o stop loss. | `70` |
| `TradeVolume` | Volume enviado com cada ordem de mercado. A alteração deste parâmetro também atualiza a base `Strategy.Volume`. | `0.1` |
| `CandleType` | Tipo de dados da vela (período de tempo) inscrito para a análise. | `1 hour` |

Todos os parâmetros numéricos expõem metadados de otimização para que a estratégia possa ser ajustada em StockSharp otimizadores, se desejado.

## Notas e recomendações
- Como a estratégia reage à expansão do corpo da vela, o período escolhido afeta significativamente a frequência do sinal. Intervalos mais curtos produzem mais negociações, mas podem exigir distâncias de proteção mais estreitas.
- Certifique-se de que a segurança conectada defina um `PriceStep` válido; caso contrário, as distâncias de proteção baseadas em degraus não poderão ser convertidas em preços absolutos.
- Faça backtest da porta dentro do StockSharp Designer antes da implantação ao vivo para validar como a parada e o destino são convertidos para o instrumento selecionado.
- A estratégia mantém uma única posição aberta por vez. Após cada saída, os contadores são redefinidos e o padrão deve ser reconstruído do zero, espelhando o comportamento original MQL4.
