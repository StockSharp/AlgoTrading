# Estratégia de Túnel Seguro FxNode
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é um port StockSharp do expert advisor do MetaTrader 4 *FxNode - Safe Tunnel*. O sistema usa um canal de tendência baseado em ZigZag: as máximas de swing mais recentes são conectadas para formar uma linha de resistência, enquanto as mínimas de swing criam uma linha de suporte. Uma posição é aberta quando o preço de mercado toca um dos limites do canal dentro de uma tolerância configurável e todas as verificações de segurança passam.

A conversão segue o fluxo de trabalho original, mas o adapta à API de alto nível do StockSharp:

- A assinatura de velas impulsiona a lógica. Apenas velas completamente formadas são processadas.
- Um par `Highest`/`Lowest` emula o detector ZigZag usado para desenhar as linhas de tendência do túnel.
- Um indicador `AverageTrueRange` fornece a âncora de stop baseada em volatilidade que a versão MQL produzia com `ATRCheck() * 10`.
- As cotações Level1 são monitoradas para que a estratégia possa impor um spread máximo antes de permitir novos trades.

## Lógica de entrada

1. Detectar máximas e mínimas de swing com uma profundidade ZigZag configurável, desvio (em pips) e backstep. As duas máximas e duas mínimas mais recentes definem as linhas de tendência.
2. Calcular o preço de cada linha de tendência no tempo de fechamento da vela atual e medir a distância vertical entre a última máxima e mínima de swing.
3. Configuração comprada: o melhor preço ask deve permanecer acima da linha de tendência inferior, mas não mais longe do que o buffer `TouchDistanceBuyPips`. As posições vendidas espelham a condição em torno da linha de tendência superior e o melhor bid.
4. O filtro de sessão opcional (padrão meia-noite–06:00) deve permitir o trading. A estratégia também bloqueia novas ordens na sexta-feira, sábado e domingo, imitando as restrições originais de `AllowToOrder()`.
5. O spread atual (ask – bid) não deve exceder `MaxSpreadPips` quando as cotações estão disponíveis.
6. `MaxOpenPositions` controla a exposição líquida máxima. Como o StockSharp usa netting, este valor age como um limite no volume total da posição em vez de em tickets separados.

## Lógica de saída

- Stop-loss inicial: o EA original o colocava em `ATR * 10`. O port mantém o mesmo multiplicador respeitando o limite `MaxStopLossPips`.
- Take-profit inicial: padrão é a distância entre a última máxima e mínima de swing, mas é limitado por `TakeProfitPips` quando configurado.
- Alvo de lucro fixo: se `FixedTakeProfitPips` for maior que zero, a posição é fechada quando o preço ganha pelo menos essa quantidade de pips a partir da entrada.
- Trailing stop: uma vez que o fechamento da vela se move mais de `TrailingStopPips` a favor do trade, o stop-loss é ajustado para travar lucros.
- Saída de fim de semana: quando `CloseBeforeWeekend` está habilitado, qualquer posição aberta é fechada após as 23:50 de sexta-feira.

Todas as saídas são executadas com ordens a mercado para permanecer consistentes com o comportamento original.

## Risco e dimensionamento

O tamanho do lote é calculado em três estágios:

1. Tentar arriscar `RiskPercentage` do valor do portfólio, assumindo que o passo de preço do instrumento e o valor monetário do passo são conhecidos.
2. Se o dimensionamento por risco não puder ser calculado, recorrer ao `StaticVolume`.
3. Limitar o volume final entre `MinVolume` e `MaxVolume`.

Como o StockSharp relata uma única posição líquida por instrumento, o limite original de `MaxOpenPosition` é interpretado como uma exposição total máxima em vez de uma contagem de tickets independentes.

## Parâmetros

| Nome | Padrão | Descrição |
|------|--------|-----------|
| `CandleType` | Velas de 30 minutos | Período principal para análise e trading. |
| `TrendPreference` | Ambos | Escolher trading somente comprado, somente vendido ou simétrico. |
| `TakeProfitPips` | 800 | Distância máxima de take-profit em pips (0 desativa o limite). |
| `MaxStopLossPips` | 200 | Distância máxima de stop-loss em pips (0 desativa o limite). |
| `FixedTakeProfitPips` | 0 | Distância de saída antecipada expressa em pips. |
| `TouchDistanceBuyPips` | 20 | Entradas compradas requerem que o preço ask permaneça dentro deste buffer acima da linha de tendência inferior. |
| `TouchDistanceSellPips` | 20 | Entradas vendidas espelham o requisito de buffer próximo à linha de tendência superior. |
| `TrailingStopPips` | 50 | Distância de trailing aplicada após o trade se tornar lucrativo. |
| `StaticVolume` | 1 | Volume de ordem alternativo quando o dimensionamento baseado em risco não é possível. |
| `MinVolume` / `MaxVolume` | 0.02 / 10 | Limites para o volume final da ordem. |
| `MaxSpreadPips` | 15 | Spread máximo permitido em pips para novas entradas. |
| `RiskPercentage` | 30 | Percentual do portfólio arriscado por trade. Definir como 0 para sempre usar `StaticVolume`. |
| `MaxOpenPositions` | 1 | Exposição líquida máxima (em múltiplos do volume de ordem atual). |
| `UseTimeFilter` | true | Habilita a janela de trading. |
| `SessionStart` / `SessionEnd` | 00:00 / 06:00 | Janela de trading. Quando o início é posterior ao fim, a janela se estende pela meia-noite. |
| `CloseBeforeWeekend` | true | Fechar qualquer posição após as 23:50 de sexta-feira. |
| `AtrPeriod` | 14 | Lookback do ATR usado para o cálculo do stop. |
| `ZigZagDepth` | 5 | Profundidade de lookback do ZigZag. |
| `ZigZagDeviationPips` | 3 | Distância mínima entre pivôs consecutivos (em pips). |
| `ZigZagBackstep` | 1 | Barras entre pivôs elegíveis. |
| `ZigZagHistory` | 10 | Número de pivôs armazenados para a projeção de linhas de tendência. |

## Notas e limitações

- A reconstrução do ZigZag espelha o comportamento MQL combinando os indicadores `Highest`/`Lowest` com filtros de desvio e backstep. Se o instrumento negocia em uma sessão personalizada, considere ajustar os parâmetros para alinhá-los com o indicador original.
- A filtragem de spread requer cotações bid/ask ao vivo. Quando as cotações estão ausentes (por exemplo, durante o backtesting com dados apenas de velas), o filtro de spread é ignorado.
- O port opera com posições líquidas. Ambientes que requerem gerenciamento independente de tickets devem estender a estratégia para rastrear cada execução separadamente.
- As strings de tempo da versão MQL (p. ex., `"24:00"`) são substituídas por parâmetros `TimeSpan`. Para reproduzir uma sessão noturna, defina o início mais tarde que o fim, por exemplo 23:30 a 05:30.

## Uso

1. Anexar a estratégia a um instrumento, configurar o tipo de vela e os parâmetros, e executá-la em modo de simulação ou ao vivo.
2. Garantir que as assinaturas de profundidade de mercado ou Level1 estejam habilitadas para aplicar o filtro de spread com precisão.
3. Revisar e ajustar os controles de risco antes de negociar com capital real.
