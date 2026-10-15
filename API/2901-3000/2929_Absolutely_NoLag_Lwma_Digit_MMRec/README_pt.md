# Estratégia AbsolutelyNoLag Lwma Digit MMRec
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Conceito

Esta estratégia é uma porta para StockSharp do especialista MetaTrader *Exp_AbsolutelyNoLagLwma_Digit_NN3_MMRec*. Mantém a arquitetura multi-timeframe original construída em torno do indicador "AbsolutelyNoLagLWMA" e reproduz as regras de recuperação de gestão monetária (`MMRec`). Três módulos independentes (A/B/C) monitoram velas de 12 horas, 4 horas e 2 horas respectivamente. Cada módulo pode abrir e fechar sua própria fatia de posição enquanto a estratégia rastreia a exposição combinada.

Cada módulo calcula uma média móvel ponderada dupla (WMA de uma WMA) de uma fonte de preço configurável. O valor suavizado é arredondado para o número solicitado de dígitos, exatamente como no indicador MQL. Uma mudança na inclinação da linha suavizada (o valor sobe após cair ou vice-versa) é tratada como uma troca de direção e gera ações de trading para esse módulo.

## Regras de Trading

1. Aguardar uma vela concluída do timeframe do módulo.
2. Ler o preço aplicado (fechamento, abertura, mediana, típico, etc.).
3. Processar o preço pelo WMA primário e alimentar seu resultado em um WMA secundário para emular "AbsolutelyNoLagLWMA".
4. Arredondar o valor suavizado para o número configurado de dígitos e compará-lo com o valor anterior.
5. **Inclinação ascendente** (`value > previous`):
   - Fechar a perna vendida do módulo se as saídas vendidas estiverem habilitadas.
   - Se as entradas compradas estiverem habilitadas e nenhuma exposição comprada estiver ativa, abrir uma posição comprada usando o volume atual do módulo.
   - Recalcular os níveis de stop-loss e take-profit (expressos em passos de preço) para a fatia comprada.
6. **Inclinação descendente** (`value < previous`):
   - Fechar a perna comprada do módulo se as saídas compradas estiverem habilitadas.
   - Se as entradas vendidas estiverem habilitadas e nenhuma exposição vendida estiver ativa, abrir uma posição vendida.
   - Atualizar os níveis protetores para a fatia vendida.
7. Em cada vela o módulo verifica se o máximo/mínimo da vela perfurou o nível atual de stop-loss ou take-profit. Se tocado, a fatia da posição é zerada a esse preço e o resultado do trade é registrado para a lógica de gestão monetária.
8. A gestão monetária mantém uma fila dos resultados de trades mais recentes para cada direção. Quando os últimos *N* trades (onde *N* é igual ao gatilho de perda) foram perdedores, a próxima ordem usa o volume reduzido; caso contrário, o volume normal é usado. A detecção de trade perdedor é baseada no preço de entrada que foi armazenado quando a fatia foi aberta e o preço de saída (stop/alvo/fechamento) usado para zerá-la.

A estratégia usa ordens a mercado para entradas e saídas e assume execuções no fechamento da vela para sinais e ao preço protetor para saídas de stop/alvo.

## Parâmetros

Cada módulo possui um conjunto idêntico de parâmetros. Os valores padrão correspondem ao especialista MQL fonte.

| Parâmetro | Descrição |
|-----------|-----------|
| `ACandleType` / `BCandleType` / `CCandleType` | Período das velas do módulo (12h / 4h / 2h por padrão). |
| `ALength` / `BLength` / `CLength` | Comprimento da suavização AbsolutelyNoLagLWMA (aplicada a ambos os WMAs). |
| `AAppliedPrice` / `BAppliedPrice` / `CAppliedPrice` | Fonte de preço usada no indicador (close, open, high, low, median, typical, weighted, simple, quarter, TrendFollow1, TrendFollow2, Demark). |
| `ADigits` / `BDigits` / `CDigits` | Número de dígitos para arredondar o valor suavizado. |
| `ABuyOpen`, `ASellOpen`, `ABuyClose`, `ASellClose` (e equivalentes dos módulos B/C) | Flags que controlam se o módulo pode abrir/fechar fatias compradas ou vendidas. |
| `ASmallVolume`, `ANormalVolume` | Volumes de ordem reduzido e normal. Os mesmos valores são reutilizados para trades vendidos. |
| `ABuyLossTrigger`, `ASellLossTrigger` | Número de trades perdedores consecutivos que ativa o volume reduzido para comprados/vendidos. |
| `AStopLossPoints`, `ATakeProfitPoints` | Níveis protetores expressos em passos de preço para a fatia do módulo. Parâmetros idênticos existem para os módulos B e C. |

As filas de gestão monetária são reiniciadas quando o gatilho correspondente é definido como zero. Os cálculos de passos de preço dependem de `Security.Step`; se o instrumento não o expõe, um passo de `1` é usado.

## Notas

- Cada módulo gerencia seu próprio volume de posição interno; portanto, diferentes módulos podem estar comprados ou vendidos simultaneamente. A posição principal da estratégia é a soma de todas as fatias dos módulos.
- Os níveis de stop-loss e take-profit são verificados em cada vela concluída usando o máximo/mínimo da vela para detectar perfurações.
- A enumeração `AppliedPrices` corresponde às opções do indicador original, incluindo ambas as fórmulas TrendFollow e a variante DeMark.
- A estratégia não adiciona indicadores ao gráfico; ela depende da API `Bind` de alto nível e mantém instâncias de indicadores privadas para cada módulo conforme exigido pelas diretrizes.
- A lógica fecha e abre trades apenas quando a inclinação muda de direção, o que previne ordens duplicadas em barras consecutivas com o mesmo estado de tendência.
