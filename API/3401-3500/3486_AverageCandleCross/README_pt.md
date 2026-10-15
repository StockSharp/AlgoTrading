# Estratégia Cruzada Média de Velas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia recria o especialista em "Cruz de vela média" MetaTrader. Ele espera por uma barra completa onde a vela anterior fechou em uma média móvel, enquanto dois filtros adicionais de média móvel já confirmam a tendência predominante. Apenas uma posição pode estar ativa por vez. Imediatamente após a abertura de uma negociação, o algoritmo anexa um stop-loss e um take-profit cuja distância é derivada do stop especificado baseado em pip. Isso torna o comportamento idêntico à lógica do bloco original que é acionado uma vez por barra.

A lógica de entrada lê os dados históricos da barra em vez de ticks inacabados, de modo que todos os sinais são avaliados no fechamento da última vela concluída. Conjuntos de parâmetros separados controlam os filtros de alta e baixa, permitindo suavização assimétrica ou durações de período. Os níveis de proteção são criados com ordens stop e limit nativas posicionadas a `StopLossPips * PipSize` de distância do preço de entrada. O take-profit reutiliza a mesma distância de parada e multiplica pelo fator percentual definido para cada lado.

## Detalhes

- **Critérios de entrada**:
  - **Longa**: Os filtros de tendência rápida e lenta para o lado longo estão subindo na barra anterior (`MA_fast1[1] > MA_slow1[1]` e `MA_fast2[1] > MA_slow2[1]`) e a vela anterior fecha acima de sua média dedicada, enquanto a vela de duas barras atrás estava abaixo dela (`Close[2] <= MA_cross[2]` e `Close[1] > MA_cross[1]`).
  - **Venda**: Os filtros de tendência rápida e lenta para o lado vendido estão diminuindo na barra anterior (`MA_fast1[1] < MA_slow1[1]` e `MA_fast2[1] < MA_slow2[1]`) e a vela anterior fecha abaixo de sua média dedicada enquanto a vela de duas barras atrás estava acima dela (`Close[2] >= MA_cross[2]` e `Close[1] < MA_cross[1]`).
- **Longo/Curto**: Ambas as direções, mas nunca simultaneamente.
- **Critérios de saída**:
  - As posições são fechadas exclusivamente pelas ordens de stop-loss de proteção ou take-profit.
- **Para**: Sim. O stop é colocado a `StopLossPips * PipSize` de distância do preço de entrada; o take-profit é igual à distância de parada multiplicada pelo parâmetro `% of SL`.
- **Valores padrão**:
  - `FirstTrendFastPeriod` = 5, `FirstTrendFastMethod` = SMA.
  - `FirstTrendSlowPeriod` = 20, `FirstTrendSlowMethod` = SMA.
  - `SecondTrendFastPeriod` = 20, `SecondTrendFastMethod` = SMA.
  - `SecondTrendSlowPeriod` = 30, `SecondTrendSlowMethod` = SMA.
  - `BullCrossPeriod` = 5, `BullCrossMethod` = SMA.
  - `BuyVolume` = 0,01, `BuyStopLossPips` = 50, `BuyTakeProfitPercent` = 100.
  - `FirstTrendBearFastPeriod` = 5, `FirstTrendBearFastMethod` = SMA.
  - `FirstTrendBearSlowPeriod` = 20, `FirstTrendBearSlowMethod` = SMA.
  - `SecondTrendBearFastPeriod` = 20, `SecondTrendBearFastMethod` = SMA.
  - `SecondTrendBearSlowPeriod` = 30, `SecondTrendBearSlowMethod` = SMA.
  - `BearCrossPeriod` = 5, `BearCrossMethod` = SMA.
  - `SellVolume` = 0,01, `SellStopLossPips` = 50, `SellTakeProfitPercent` = 100.
  - `PipSize` = 0,0001.
- **Filtros**:
  - Categoria: Acompanhamento de tendências.
  - Direção: Dupla (longa + curta).
  - Indicadores: Múltiplas médias móveis.
  - Stops: Stop fixo baseado em pip e take-profit proporcional.
  - Complexidade: Moderada.
  - Prazo: Funciona na série de velas configurada (padrão 15 minutos).
  - Sazonalidade: Não.
  - Redes neurais: Não.
  - Divergência: Não.
  - Nível de risco: Médio.
