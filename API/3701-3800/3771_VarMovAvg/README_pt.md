# Estratégia VarMovAvg
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia VarMovAvg é um sistema de parada e reversão convertido do MetaTrader 4 consultor especialista `VarMovAvg_v0011`. Ele usa uma média móvel variável adaptativa (VMA) para medir a direção da tendência e aguarda um padrão de retrocesso de duas etapas (chamado de Barra A e Barra B no EA original) antes de reverter a posição. Enquanto uma posição está ativa, um trailing stop baseado na média móvel protege os lucros e inverte a negociação quando a sequência oposta da Barra A/Barra B é concluída.

## Lógica de negociação
1. **VMA adaptativo** – O indicador personalizado `VariableMovingAverage` replica a fórmula MT4:
   - O índice de eficiência compara o fechamento atual com o fechamento de `AmaPeriod` barras atrás e o divide pelo movimento absoluto acumulado do preço.
   - O coeficiente de suavização interpola entre os períodos rápido e lento e é elevado ao parâmetro `SmoothingPower` exatamente como o valor original `G`.
2. **Detecção de sinal (Barra A/Barra B)** – Duas máquinas de estado independentes rastreiam configurações longas e curtas:
   - *Barra A*: O preço se move `SignalPipsBarA` (em pips) além do VMA na direção de negociação potencial.
   - *Barra B*: O preço estende mais `SignalPipsBarB` pips na mesma direção, travando o preço extremo.
   - *Entrada*: Quando o fechamento retorna à banda de entrada definida por `SignalPipsTrade ± EntryPipsDiff`, a estratégia entra (ou reverte) usando ordens de mercado.
3. **Trailing Stop e Reversão** – Enquanto uma posição está aberta, uma média móvel calculada em máximas (para vendas) ou mínimas (para longas) é deslocada por `StopMaShift` barras e preenchida por `StopPipsDiff`.
   - Se a vela ultrapassar o nível de stop, a posição será fechada.
   - Se a sequência oposta da Barra A/Barra B for acionada enquanto uma posição existir, a estratégia emite uma única ordem de mercado dimensionada como `|Position| + Volume` para mudar de direção imediatamente, correspondendo ao comportamento EA.

## Parâmetros
| Parâmetro | Descrição | Fonte MT4 |
|-----------|-------------|------------|
| `AmaPeriod` | Janela de lookback usada pelo VMA. | `prm.vma.periodAMA` |
| `FastPeriod` | Fator de suavização rápido dentro do VMA. | `prm.vma.nfast` |
| `SlowPeriod` | Fator de suavização lento dentro do VMA. | `prm.vma.nslow` |
| `SmoothingPower` | Expoente `G` aplicado ao coeficiente adaptativo. | `prm.vma.G` |
| `SignalPipsBarA` | Distância do VMA necessária para aceitar a Barra A. | `prm.sig.pipsBarA` |
| `SignalPipsBarB` | Distância adicional necessária para aceitar a Barra B. | `prm.sig.pipsBarB` |
| `SignalPipsTrade` | Deslocamento do extremo da Barra B até a linha de entrada. | `prm.sig.pipsTrade` |
| `EntryPipsDiff` | Tolerância aceita em torno da linha de entrada. | `prm.entry.diff` |
| `StopPipsDiff` | Offset aplicado à média móvel do trailing stop. | `prm.stop.diff` |
| `StopMaPeriod` | Período da média móvel stop. | `prm.mastop.period` |
| `StopMaShift` | Mudança (barras) da média móvel stop. | `prm.mastop.shift` |
| `StopMaMethod` | Método de média móvel (`MODE_SMA`, `EMA`, `SMMA`, `LWMA`). | `prm.mastop.method` |
| `CandleType` | Prazo de trabalho. | Prazo do gráfico |

> **Conversão de pip** – Todas as distâncias de pip são multiplicadas por `Security.PriceStep` quando estão disponíveis. Caso o instrumento não possua uma etapa configurada, os valores brutos são interpretados em unidades de preço, replicando o fallback EA.

## Notas de uso
- A estratégia depende de `SubscribeCandles` e funciona inteiramente em velas acabadas; a lógica da banda de entrada reflete as verificações tick-by-tick do EA usando preços de fechamento.
- As ordens de proteção são modeladas por meio de saídas de mercado quando a vela cruza o nível de stop, o que corresponde ao comportamento EA porque as ordens de stop foram recalculadas a cada tick.
- A mudança da média móvel é implementada por meio de um buffer FIFO, garantindo que `StopMaShift = 0` use o valor mais recente e que as mudanças positivas analisem o número solicitado de barras.
- Após cada negociação (entrada, reversão ou stop hit), ambos os rastreadores de sinal são redefinidos para o estado neutro para evitar ordens duplicadas, emulando a lógica de redefinição `STATUS_TRADE` em MetaTrader.

## Início rápido
1. Adicione a estratégia a um ambiente StockSharp e atribua um instrumento com um `PriceStep` e tamanho de tick válidos.
2. Configure o período por meio de `CandleType` (o especialista original foi testado em gráficos intradiários como M5).
3. Ajuste as distâncias do pip e os parâmetros finais para corresponder à precisão da cotação do corretor.
4. Inicie a estratégia; ele alternará entre posições longas e curtas sempre que as condições da Barra A/Barra B forem atendidas.

## Diferenças do original EA
- A versão StockSharp funciona em velas fechadas em vez de execução tick-by-tick. A banda de tolerância de entrada mantém o tempo de disparo próximo ao comportamento do MT4.
- O tratamento de stop-loss é implementado verificando os extremos das velas em vez de colocar/modificar ordens MT4, porque as estratégias StockSharp normalmente gerenciam as saídas de forma programática.
- O indicador `VariableMovingAverage` é implementado diretamente em C# e expõe o poder de suavização, eliminando o parâmetro `dK` não utilizado que existia na fonte MQL.
