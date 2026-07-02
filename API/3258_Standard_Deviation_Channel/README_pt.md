# Estratégia de Standard Deviation Channel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é um port StockSharp do especialista MetaTrader **Standard Deviation Channel**. Ela traça um canal de volatilidade baseado em uma média móvel ponderada linear (LWMA) e opera rompimentos alinhados com a tendência predominante. As entradas são filtradas pela força do Momentum e uma confirmação do MACD, enquanto as saídas combinam alvos fixos, saltos de break-even e proteção por trailing.

## Indicadores e sinais
- **Canal de desvio padrão** construído a partir de uma linha base LWMA e um multiplicador de desvio configurável. Configurações longas exigem que a banda superior suba; configurações curtas exigem que a banda inferior desça.
- **Filtro de tendência:** LWMA rápida e lenta calculadas nas mesmas velas. Posições longas exigem `LWMA_fast > LWMA_slow`; posições curtas requerem o oposto.
- **Filtro de Momentum:** um indicador de Momentum de 14 períodos. Pelo menos uma das últimas três leituras deve desviar do nível neutro de 100 pelo limiar configurado.
- **Filtro MACD:** configuração clássica 12/26/9. Entradas longas precisam de `MACD ≥ signal`, enquanto entradas curtas requerem `MACD ≤ signal`.

## Gerenciamento de operações
- **Dimensionamento de posição:** usa o parâmetro `TradeVolume`. Reversões fecham automaticamente a exposição oposta antes de abrir o novo lado.
- **Take-profit e stop-loss:** expressos em pips e avaliados contra o `PriceStep` do instrumento. A estratégia emite saídas de mercado quando o intervalo da vela toca o preço alvo ou de stop.
- **Salto de break-even:** quando o lucro não realizado atinge `BreakEvenTriggerPips`, o stop é movido para a entrada mais `BreakEvenOffsetPips` (ou menos para vendidos).
- **Trailing stop:** após atingir `TrailingStartPips`, o stop segue o preço por `TrailingStepPips`, assegurando ganhos em ambos os lados.
- **Saída por rejeição do canal:** se o preço fechar de volta dentro do canal e a inclinação achatar contra a posição, a operação é fechada antecipadamente.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `CandleType` | Período principal utilizado para todos os cálculos. |
| `TradeVolume` | Tamanho base da ordem. |
| `TrendLength` | Período de retrocesso LWMA que define a linha base do canal. |
| `DeviationMultiplier` | Multiplicador de desvio padrão para a largura do canal. |
| `FastMaLength` / `SlowMaLength` | Comprimentos LWMA para o filtro de tendência. |
| `MomentumPeriod` | Período de retrocesso para o filtro de Momentum. |
| `MomentumThreshold` | Desvio mínimo de 100 exigido em qualquer um dos últimos três valores de Momentum. |
| `TakeProfitPips` / `StopLossPips` | Distância dos níveis de saída fixos (convertidos usando `PriceStep`). |
| `BreakEvenTriggerPips` / `BreakEvenOffsetPips` | Controla quando e como o stop de break-even é ativado. |
| `TrailingStartPips` / `TrailingStepPips` | Ativa e dimensiona o trailing stop. |
| `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` | Configuração do MACD. |
| `MaxPositionUnits` | Posição líquida absoluta máxima; previne alavancagem excessiva. |

## Notas de uso
1. Anexe a estratégia a um ativo que exponha um `PriceStep` válido. Os pips são convertidos multiplicando este valor de passo.
2. Use `TrendLength` e `DeviationMultiplier` para adaptar o canal a diferentes mercados.
3. Os filtros de Momentum e MACD podem ser relaxados (limiar menor, períodos mais curtos) para aumentar a frequência de operações.
4. A lógica de trailing funciona em fechamentos de velas; picos intrabarra que não terminam além dos limiares são ignorados.

## Diferenças em relação ao Expert Advisor original
- A versão MetaTrader depende de objetos gráficos para ler a inclinação do canal e usa vários ramos de gerenciamento de dinheiro (dimensionamento martingale, proteção de capital). Este port mantém a verificação de inclinação, mas simplifica o controle de risco para operações de tamanho fixo limitadas por `MaxPositionUnits`.
- Todas as saídas são tratadas com ordens de mercado no fechamento da vela, pois as estratégias StockSharp não espelham diretamente as APIs de modificação de ordens do MT4.
- Notificações por e-mail e push são substituídas por mensagens `AddInfoLog` para manter a conversão autocontida.
- Cortes de conta baseados em capital foram omitidos; em vez disso, o foco é posto nas funcionalidades de proteção por posição.

## Aviso legal
Esta amostra destina-se a uso educacional. Sempre faça testes prospectivos e valide a configuração antes de implantá-la em uma conta real.
