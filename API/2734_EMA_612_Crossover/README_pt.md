# Estratégia Ema612CrossoverStrategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Resumo
- Port do consultor especializado do MetaTrader 5 **"EMA 6.12 (edição de barabashkakvn)"** para a API de alto nível do StockSharp.
- Negocia o cruzamento entre uma média móvel simples rápida e uma lenta (o script original também usava MODE_SMA apesar de seu nome EMA).
- Adiciona gerenciamento opcional de take profit e trailing stop expressos em unidades de preço absolutas para que o comportamento possa ser ajustado por instrumento.

## Lógica de trading
### Preparação de dados
- A estratégia subscreve velas do tipo definido por `CandleType` (período de 15 minutos por padrão).
- Duas médias móveis simples são calculadas: comprimento `FastPeriod` para a curva rápida e comprimento `SlowPeriod` para a curva lenta. O período lento deve ser maior que o período rápido.

### Regras de entrada
- Os sinais são avaliados no fechamento de cada vela terminada.
- Um **cruzamento altista** ocorre quando a SMA lenta estava acima da SMA rápida na vela anterior e cai abaixo dela na vela atual. Qualquer posição vendida aberta é fechada e uma posição comprada é aberta com o `Volume` configurado.
- Um **cruzamento baixista** ocorre quando a SMA lenta estava abaixo da SMA rápida na vela anterior e sobe acima dela na vela atual. Qualquer posição comprada aberta é fechada e uma posição vendida é aberta com o `Volume` configurado.

### Regras de saída
- As posições abertas são fechadas no cruzamento oposto conforme descrito acima.
- Take profit opcional: se `TakeProfitOffset` for maior que zero, a estratégia calcula um alvo de preço fixo a partir do preço de entrada. Trades comprados saem quando o preço atinge `entrada + TakeProfitOffset`; trades vendidos saem quando o preço atinge `entrada - TakeProfitOffset`.
- Trailing stop opcional: quando `TrailingStopOffset` for maior que zero, a estratégia aguarda até que o lucro não realizado ultrapasse `TrailingStopOffset + TrailingStepOffset`. Uma vez que esse limiar é cruzado, o preço de stop é ajustado para ficar `TrailingStopOffset` afastado do último fechamento, mas somente se o novo nível estiver pelo menos `TrailingStepOffset` mais próximo do preço do que o stop anterior. Trades comprados usam mínimas para acionar o stop, vendidos usam máximas.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `CandleType` | Período de 15 minutos | Resolução de vela usada para cálculos SMA e avaliação de sinais. |
| `FastPeriod` | 6 | Período para a média móvel simples rápida. Deve ser > 0 e menor que `SlowPeriod`. |
| `SlowPeriod` | 54 | Período para a média móvel simples lenta. Deve ser > 0 e maior que `FastPeriod`. |
| `Volume` | 1 | Volume de ordem usado para novas entradas. |
| `TakeProfitOffset` | 0.001 | Distância de preço absoluta opcional para o alvo de take profit. Definir como 0 para desabilitar. |
| `TrailingStopOffset` | 0.005 | Distância absoluta entre o preço e o trailing stop. Definir como 0 para desabilitar o trailing. |
| `TrailingStepOffset` | 0.0005 | Movimento favorável adicional necessário antes de o trailing stop ser movido. |

> **Importante:** os offsets são especificados em unidades de preço absolutas. Ajuste-os para corresponder ao tamanho do tick do instrumento (por exemplo, no EURUSD com um passo de 0.0001, os valores padrão correspondem a 10, 50 e 5 pips respectivamente).

## Notas de implementação
- Usa o fluxo de trabalho de alto nível `SubscribeCandles().Bind()` conforme exigido pelas diretrizes do projeto.
- A saída do gráfico plota ambas as SMAs e marcadores de operações quando os gráficos estão disponíveis no ambiente.
- As variáveis de estado rastreiam o preço de entrada, o nível de trailing stop e o alvo de take profit exatamente como a versão MQL.
- A implementação em C# impõe `SlowPeriod > FastPeriod` na inicialização para evitar uma configuração de indicador inválida.

## Dicas de uso
- Otimize o período das velas e os períodos SMA para corresponder ao mercado sendo negociado (p.ex., períodos mais curtos para futuros intradiários, mais longos para swing trading).
- Converta os offsets de pips ou ticks em unidades de preço absolutas antes de executar a estratégia.
- O trailing pode ser desativado definindo `TrailingStopOffset` como zero; a estratégia então dependerá exclusivamente do cruzamento oposto ou do take profit opcional para as saídas.
