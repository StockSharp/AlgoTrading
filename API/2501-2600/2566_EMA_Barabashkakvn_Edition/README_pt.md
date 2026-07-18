# Estratégia EMA (Edição barabashkakvn)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Convertida do consultor especialista MetaTrader 5 "EMA (barabashkakvn's edition)". O sistema opera o cruzamento de duas médias móveis exponenciais calculadas sobre o preço mediana e usa níveis virtuais de take-profit/stop-loss expressos em pips. As posições só são abertas após um cruzamento confirmado e um pequeno retrocesso em direção ao extremo da vela anterior.

## Ideia central

1. Rastrear EMAs de 5 e 10 períodos (preço mediana) no período selecionado.
2. Quando a EMA rápida cruza a EMA lenta, armar um sinal pendente em vez de operar imediatamente.
3. Aguardar o preço retrair `MoveBackPips` a partir do extremo da vela anterior enquanto o spread da EMA excede `2 * pipSize`.
4. Entrar na direção do cruzamento assim que o retrocesso ocorrer.
5. Gerenciar a posição aberta com alvos e stops virtuais medidos em pips a partir do preço de entrada.

Este comportamento reflete a implementação MQL original: o especialista aguardava a sinalização de cruzamento (`check`) e então exigia um spread de EMA mais um retrocesso de preço relativo à vela anterior para acionar a operação. As regras de saída também seguem a abordagem "virtual" fechando posições quando o bid/ask teria tocado as distâncias especificadas.

## Indicadores e dados

- EMA de 5 períodos sobre o preço mediana (high + low) / 2.
- EMA de 10 períodos sobre o preço mediana.
- Máximo/mínimo da vela terminada anterior para verificações de retrocesso.
- Todo o processamento usa velas terminadas da subscrição `CandleType` configurada.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `OrderVolume` | `0.1` | Volume de negociação em lotes/contratos para cada entrada. |
| `VirtualProfitPips` | `5` | Distância (em pips) entre o preço de entrada e o take-profit virtual. |
| `MoveBackPips` | `3` | Retrocesso necessário após o cruzamento, medido a partir do extremo da vela anterior. |
| `StopLossPips` | `20` | Distância (em pips) entre o preço de entrada e o stop loss virtual. |
| `PipSize` | `0.0001` | Tamanho do pip expresso em unidades de preço. Substituir ao negociar símbolos com uma definição de pip diferente. |
| `FastLength` | `5` | Comprimento da EMA rápida. |
| `SlowLength` | `10` | Comprimento da EMA lenta. |
| `CandleType` | `TimeFrame(1m)` | Fonte de velas usada para cálculos. |

Todos os valores baseados em pips são convertidos para distâncias de preço usando `pipValue = PipSize`. Se o parâmetro for deixado em zero ou número negativo, a estratégia recorre ao `Security.PriceStep` (quando fornecido pelo board).

## Lógica de negociação

### Condições de entrada

- **Armamento do sinal**: armazenar um sinal pendente sempre que um cruzamento ocorrer (`FastEMA` cruza acima de `SlowEMA` ou vice-versa). Nenhuma operação é colocada ainda.
- **Entrada vendida**: requer
  - Sinal pendente presente.
  - `SlowEMA - FastEMA > 2 * pipSize`.
  - Máxima da vela atual ≥ mínima da vela anterior + `MoveBackPips * pipSize` (preço retrocedeu para cima a partir da mínima anterior).
- **Entrada comprada**: requer
  - Sinal pendente presente.
  - `FastEMA - SlowEMA > 2 * pipSize`.
  - Mínima da vela atual ≤ máxima da vela anterior - `MoveBackPips * pipSize` (preço retrocedeu para baixo a partir da máxima anterior).

Após abrir uma posição a bandeira pendente é redefinida para evitar entradas duplicadas.

### Condições de saída

Os alvos virtuais emulam o comportamento MQL comparando os extremos da vela com as distâncias predefinidas:

- **Posição comprada**:
  - Fechar se a máxima da vela ≥ preço de entrada + `VirtualProfitPips * pipSize`.
  - Fechar se a mínima da vela ≤ preço de entrada - `StopLossPips * pipSize`.
- **Posição vendida**:
  - Fechar se a mínima da vela ≤ preço de entrada - `VirtualProfitPips * pipSize`.
  - Fechar se a máxima da vela ≥ preço de entrada + `StopLossPips * pipSize`.

Após qualquer saída os níveis virtuais são redefinidos e a estratégia aguarda o próximo cruzamento.

## Notas de implementação

- Usa a subscrição de velas de alto nível (`SubscribeCandles`) e desenha EMAs mais operações na área de gráfico opcional.
- O preço mediana é calculado diretamente a partir do high/low da vela para corresponder a `PRICE_MEDIAN` do MetaTrader.
- A sinalização de cruzamento (`_hasCrossSignal`) reproduz a variável `check` original, garantindo que as operações só ocorram após verificações de cruzamento e retrocesso.
- `StartProtection()` é chamado em `OnStarted` para habilitar o monitoramento de risco integrado mesmo que a estratégia gerencie as saídas manualmente.
- O código mantém todos os comentários em inglês, conforme solicitado, e depende apenas de velas terminadas sem acessar diretamente os buffers de indicadores.

## Dicas de uso

- Ajustar `PipSize` ao operar instrumentos com definições de pip não-padrão (p.ex., pares JPY, índices, cotações cripto).
- Como as saídas dependem dos extremos das velas, usar períodos mais curtos (1–5 minutos) mantém o comportamento mais próximo do especialista original baseado em ticks.
- A otimização pode explorar comprimentos de EMA, distâncias em pips e valores de retrocesso usando os metadados de parâmetros fornecidos.
- A estratégia opera uma posição de cada vez; qualquer posição externa no mesmo instrumento pode interferir no rastreamento virtual de saídas.

## Riscos

- A simulação baseada em velas pode perder toques intrabar dos níveis virtuais; considerar dados de maior resolução se a precisão for crítica.
- As saídas virtuais não colocam ordens protetoras reais, portanto desconexões ou slippage podem levar a perdas maiores do que o esperado no trading ao vivo.
- Como em qualquer sistema de cruzamento, o desempenho degrada em mercados laterais; combinar com filtros se necessário.
