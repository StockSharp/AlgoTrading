# Estratégia Color Schaff JJRSX MMRec Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é um port para StockSharp do expert advisor MetaTrader `Exp_ColorSchaffJJRSXTrendCycle_MMRec_Duplex`. O robô original combina dois osciladores Schaff Trend Cycle alimentados por momentum JJRSX e um módulo MMRec (Recalculação de Gestão Monetária) que reduz a exposição após uma sequência de perdas. A conversão em C# preserva o layout duplo comprado/vendido e espelha os controles de risco ajustáveis, enquanto substitui o indicador JJRSX indisponível por uma aproximação robusta na plataforma.

## Lógica de negociação
- Dois osciladores independentes são calculados em marcos temporais selecionados pelo usuário: um governa entradas compradas, o outro governa entradas vendidas. Cada oscilador usa linhas de momentum de estilo RSX rápidas e lentas, suavizadas e normalizadas com um pipeline de Schaff Trend Cycle para produzir valores no intervalo [-100, 100].
- Uma posição comprada é aberta quando o oscilador comprado cruza para baixo através de zero (`previous > 0` e `current <= 0`). O expert original marca esses eventos como reversões de momentum altista. As saídas compradas são acionadas sempre que o valor do indicador um bar antes é negativo.
- Uma posição vendida é aberta quando o oscilador vendido cruza para cima através de zero (`previous < 0` e `current >= 0`). As saídas vendidas são acionadas sempre que o valor do indicador um bar antes é positivo.
- A configuração `SignalBar` reproduz o comportamento do MetaTrader de avaliar sinais em barras históricas. Por exemplo, `SignalBar = 1` inspeciona o último candle completamente fechado e o candle antes dele. A estratégia mantém históricos de indicadores em movimento para emular as chamadas `CopyBuffer` do código MQL.

## Gestão monetária (MMRec)
- Blocos de gestão monetária separados são mantidos para operações compradas e vendidas. O volume base é igual a `Strategy.Volume * MM`, onde `MM` é o multiplicador normal configurável (`LongMm`/`ShortMm`).
- Após cada operação fechada, a estratégia registra se o resultado foi lucrativo ou não (com base nos preços dos candles de entrada/saída, idêntico à lógica do EA que rastreia o histórico via `HistorySelect`).
- Se as últimas `TotalTrigger` operações contiverem pelo menos `LossTrigger` perdedores, a próxima ordem para esse lado muda para o multiplicador reduzido (`SmallMm`). Quando a condição de perda desaparece, o multiplicador base é restaurado automaticamente.
- Os reversais de posição respeitam as regras MMRec: a mudança de comprado para vendido (ou vice-versa) primeiro finaliza o resultado da operação existente e atualiza os contadores de perdas antes de dimensionar a nova ordem.

## Aproximação do indicador
O robô original depende de um indicador `ColorSchaffJJRSXTrendCycle` personalizado construído sobre o oscilador JJRSX e as bibliotecas de suavização Jurik. O StockSharp não inclui esses componentes, então a conversão implementa `ColorSchaffJjrsxTrendCycleIndicator`:
- Uma aproximação RSI leve (`SimpleRsi`) calcula a linha de base de momentum com suavização exponencial idêntica ao período de suavização do EA.
- As curvas RSI rápidas e lentas são subtraídas para obter uma série semelhante ao MACD que é então normalizada em uma janela cíclica e duplamente suavizada com um fator configurável (padrão 0.5) para imitar o comportamento de Schaff Trend Cycle.
- O indicador aceita as mesmas fontes de preço (fechamento, abertura, máximo, mínimo, mediano, típico, ponderado, etc.) e retém os parâmetros de ciclo/comprimento para que os fluxos de trabalho de otimização permaneçam fiéis à estratégia de origem.

## Parâmetros
| Grupo | Nome | Descrição |
| --- | --- | --- |
| Long | `LongCandleType` | Tipo de candle ou período usado para o indicador comprado. |
| Long | `LongTotalTrigger` | Número de operações compradas concluídas inspecionadas ao avaliar o contador de perdas. |
| Long | `LongLossTrigger` | Número mínimo de perdas na janela inspecionada que ativa o multiplicador reduzido. |
| Long | `LongSmallMm` | Multiplicador de volume reduzido após perdas repetidas. |
| Long | `LongMm` | Multiplicador de volume comprado padrão. |
| Long | `LongEnableOpen` | Habilita entradas compradas. |
| Long | `LongEnableClose` | Habilita saídas compradas. |
| Long | `LongFastLength` | Aproximação do período JJRSX rápido. |
| Long | `LongSlowLength` | Aproximação do período JJRSX lento. |
| Long | `LongSmooth` | Comprimento de suavização exponencial aplicado antes da normalização de Schaff. |
| Long | `LongCycleLength` | Janela de ciclo usada para normalização mín/máx. |
| Long | `LongSignalBar` | Deslocamento histórico usado ao analisar sinais comprados. |
| Long | `LongAppliedPrice` | Fonte de preço usada pelo indicador comprado. |
| Short | `ShortCandleType` | Tipo de candle ou período usado para o indicador vendido. |
| Short | `ShortTotalTrigger` | Número de operações vendidas concluídas ao avaliar o contador de perdas. |
| Short | `ShortLossTrigger` | Número mínimo de perdas na janela inspecionada que ativa o multiplicador reduzido. |
| Short | `ShortSmallMm` | Multiplicador de volume reduzido após perdas repetidas. |
| Short | `ShortMm` | Multiplicador de volume vendido padrão. |
| Short | `ShortEnableOpen` | Habilita entradas vendidas. |
| Short | `ShortEnableClose` | Habilita saídas vendidas. |
| Short | `ShortFastLength` | Aproximação do período JJRSX rápido para vendidos. |
| Short | `ShortSlowLength` | Aproximação do período JJRSX lento para vendidos. |
| Short | `ShortSmooth` | Comprimento de suavização exponencial antes da normalização de Schaff para vendidos. |
| Short | `ShortCycleLength` | Janela de ciclo para normalização mín/máx no lado vendido. |
| Short | `ShortSignalBar` | Deslocamento histórico ao analisar sinais vendidos. |
| Short | `ShortAppliedPrice` | Fonte de preço usada pelo indicador vendido. |

## Notas de implementação
- A estratégia usa as assinaturas de candles de alto nível do StockSharp e evita acesso direto aos buffers do indicador, seguindo as diretrizes de conversão.
- As proteções (`StopLoss`/`TakeProfit`) da versão MQL não são portadas porque o MetaTrader usa distâncias baseadas em pontos; os usuários podem anexar `StartProtection` ou módulos de risco personalizados, se necessário.
- O histórico de operações é avaliado usando preços de fechamento de candles, o que reflete a dependência do EA em registros históricos de negócios, mantendo a lógica determinista dentro do StockSharp.
- O indicador personalizado expõe `IsFormed` para que a estratégia reaja apenas quando dados suficientes tenham se acumulado, evitando sinais prematuros durante o aquecimento.

## Aviso
Este port replica a estrutura lógica da estratégia MetaTrader, mas o desempenho pode diferir devido a feeds de dados, políticas de execução e a aproximação JJRSX. Valide sempre o comportamento em dados de demonstração antes de implantá-lo ao vivo.
