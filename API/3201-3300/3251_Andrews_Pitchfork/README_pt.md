# Estratégia de Andrews Pitchfork
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Porte do consultor especialista MetaTrader "Andrew's Pitchfork". O script original esperava um objeto Andrews Pitchfork desenhado manualmente e o combinava com filtros de Momentum, médias móveis de múltiplos períodos e MACD. A versão StockSharp mantém o conjunto de indicadores, substitui o desenho manual por detecção automática de tendência e recria a lógica de proteção (limites de múltiplas entradas, stop-loss, take-profit, ponto de equilíbrio e gerenciamento de trailing).

## Lógica da estratégia

1. **Indicadores**
   - Duas *Médias Móveis Ponderadas Linearmente* (LWMA) calculadas sobre o preço típico da série de candles selecionada.
   - Um oscilador *Momentum* no mesmo período, avaliado pelo desvio absoluto do nível de equilíbrio 100.
   - Um par de linhas de sinal *MACD (12, 26, 9)* clássico.
2. **Regras de entrada**
   - Trades **longos** requerem que a LWMA rápida esteja acima da LWMA lenta, pelo menos um dos últimos três desvios de Momentum exceda o `MomentumBuyThreshold`, e a linha MACD esteja acima de sua linha de sinal.
   - Trades **curtos** invertem essas condições.
   - A estratégia faz pirâmide adicionando repetidamente o `Volume` base enquanto a posição absoluta estiver abaixo de `Volume * MaxPyramids`. Sinais opostos fecham a exposição atual antes de abrir a nova direção.
3. **Gestão de risco**
   - Os níveis iniciais de stop-loss e take-profit são colocados em passos de preço ao redor da entrada. Ambos são atualizados quando o tamanho da posição muda.
   - A lógica de ponto de equilíbrio move o stop após o preço percorrer um número configurável de passos a favor da posição.
   - A lógica do trailing stop continua seguindo o preço mais lucrativo com uma distância de preenchimento adicional.

Em comparação com a versão MQL, o porte StockSharp infere automaticamente a tendência usando a inclinação da LWMA em vez de verificar a orientação de um objeto Pitchfork desenhado pelo usuário. Todos os outros filtros (Momentum, MACD, limite de múltiplas ordens) e ferramentas de gestão de dinheiro foram reproduzidos com a API de alto nível do StockSharp.

## Parâmetros

| Nome | Tipo | Padrão | Descrição |
|------|------|---------|-------------|
| `CandleType` | `DataType` | Período de 15 minutos | Série de candles principal usada por todos os indicadores. |
| `FastMaPeriod` | `int` | 6 | Comprimento da LWMA rápida sobre o preço típico. |
| `SlowMaPeriod` | `int` | 85 | Comprimento da LWMA lenta sobre o preço típico. |
| `MomentumPeriod` | `int` | 14 | Retrospectiva do indicador Momentum. |
| `MomentumBuyThreshold` | `decimal` | 0.3 | Mínimo \|Momentum - 100\| para entradas longas. |
| `MomentumSellThreshold` | `decimal` | 0.3 | Mínimo \|Momentum - 100\| para entradas curtas. |
| `MaxPyramids` | `int` | 1 | Número máximo de lotes base permitidos na mesma direção. |
| `StopLossSteps` | `int` | 20 | Distância do stop-loss em passos de preço. |
| `TakeProfitSteps` | `int` | 50 | Distância do take-profit em passos de preço. |
| `EnableTrailing` | `bool` | `true` | Habilita o trailing stop dinâmico. |
| `TrailingTriggerSteps` | `int` | 40 | Lucro em passos necessário antes de o trailing stop ser ativado. |
| `TrailingDistanceSteps` | `int` | 40 | Distância em passos mantida entre o extremo de preço e o trailing stop. |
| `TrailingPadSteps` | `int` | 10 | Preenchimento extra aplicado ao trailing stop. |
| `EnableBreakEven` | `bool` | `true` | Habilita o ajuste do stop ao ponto de equilíbrio. |
| `BreakEvenTriggerSteps` | `int` | 30 | Lucro em passos necessário antes de mover o stop para o ponto de equilíbrio. |
| `BreakEvenOffsetSteps` | `int` | 30 | Offset em passos além da entrada quando o ponto de equilíbrio é aplicado. |

## Notas

- A estratégia requer um `PriceStep` válido do ativo selecionado para converter distâncias baseadas em passos em preços. Se o passo estiver faltando, a lógica de trailing e ponto de equilíbrio permanece inativa.
- Ordens protetoras (stop e take-profit) são recriadas quando o tamanho da posição muda, garantindo que o escalonamento ou reversão alinhe as ordens com a nova exposição.
- Os parâmetros padrão correspondem à configuração original do EA, mas podem ser otimizados por meio dos intervalos `StrategyParam` integrados.
