# Estratégia Assistente MACD Parabolic SAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
Esta estratégia é uma conversão do StockSharp do Consultor Especializado do MetaTrader gerado pelo Assistente MQL5 que combina o momentum do MACD com a direção de tendência do Parabolic SAR. A lógica reproduz o mecanismo de pontuação do assistente atribuindo uma pontuação normalizada (0..100) a cada indicador e depois ponderando as contribuições antes de tomar decisões de trading.

## Lógica de Trading
- **Indicadores**
  - *MACD (12, 24, 9)*: o sinal do histograma define se o momentum altista (histograma > 0) ou o momentum baixista (histograma < 0) está ativo.
  - *Parabolic SAR (0.02, 0.2)*: o preço de fechamento acima do ponto SAR é interpretado como tendência de alta, e abaixo do ponto SAR como tendência de baixa.
- **Construção de pontuação**
  - MACD produz 100 (altista) ou 0 (baixista) pontos para o lado comprado. Os valores inversos são usados para o lado vendido.
  - O Parabolic SAR se comporta da mesma forma, fornecendo 100 pontos quando a tendência concorda com a direção respectiva.
  - Ambas as pontuações são combinadas por meio dos pesos definidos pelo usuário (`MacdWeight` e `SarWeight`). Com os pesos padrão (0.9 e 0.1), o MACD domina a decisão final assim como no modelo do assistente.
- **Regras de entrada**
  - Calcular a pontuação altista: `bullScore = macdBull * MacdWeight + sarBull * SarWeight`.
  - Calcular a pontuação baixista: `bearScore = macdBear * MacdWeight + sarBear * SarWeight`.
  - Abrir uma posição comprada (ou reverter do vendido) quando `bullScore >= OpenThreshold` (padrão `20`).
  - Abrir uma posição vendida (ou reverter do comprado) quando `bearScore >= OpenThreshold`.
- **Regras de saída**
  - Posições compradas são fechadas quando a pontuação baixista atinge o nível de confirmação forte `CloseThreshold` (padrão `100`).
  - Posições vendidas são fechadas quando a pontuação altista atinge `CloseThreshold`.
  - Os sinais de saída são avaliados antes dos sinais de entrada para imitar o comportamento do consultor especializado original que prioriza o fechamento de trades conflitantes.

## Gestão de Risco
- `StopLossPoints` e `TakeProfitPoints` replicam a gestão do dinheiro baseada em pontos do assistente. Ambos os valores são convertidos para unidades de preço usando o `PriceStep` do instrumento e então passados para `StartProtection`.
- Defina qualquer parâmetro como `0` para desabilitar a ordem de proteção correspondente.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `MacdFastPeriod` | Período de EMA rápida para MACD. | 12 |
| `MacdSlowPeriod` | Período de EMA lenta para MACD. | 24 |
| `MacdSignalPeriod` | Período de SMA de sinal para MACD. | 9 |
| `MacdWeight` | Peso da pontuação MACD (0..1). | 0.9 |
| `SarWeight` | Peso da pontuação do Parabolic SAR (0..1). | 0.1 |
| `OpenThreshold` | Pontuação mínima para abrir/reverter posições. | 20 |
| `CloseThreshold` | Pontuação oposta mínima para sair de posições. | 100 |
| `SarStep` | Passo de aceleração do Parabolic SAR. | 0.02 |
| `SarMax` | Aceleração máxima do Parabolic SAR. | 0.2 |
| `StopLossPoints` | Distância do stop-loss em pontos de preço. | 50 |
| `TakeProfitPoints` | Distância do take-profit em pontos de preço. | 115 |
| `CandleType` | Fonte de dados de velas para cálculos de indicadores. | Período de 15 minutos |

## Notas de Uso
- Os parâmetros padrão espelham o template `.mq5`, portanto a estratégia se comporta de forma consistente com o consultor especializado original gerado pelo assistente.
- Ajuste `MacdWeight`, `SarWeight` e os limiares para mudar a sensibilidade das entradas e saídas. Por exemplo, aumentar `OpenThreshold` exigirá confirmação mais forte antes de abrir novos trades.
- Os campos internos `_lastBullScore` e `_lastBearScore` são atualizados a cada barra e podem ser registrados ou expostos se você precisar monitorar como a pontuação combinada evolui ao longo do tempo.
- Como a estratégia depende de velas concluídas, certifique-se de que seu feed de dados fornece atualizações completas de velas para o `CandleType` selecionado.
- A gestão do dinheiro é expressa em pontos; certifique-se de que o instrumento escolhido usa o passo de preço esperado para que as ordens de proteção se alinhem com as distâncias pretendidas.
