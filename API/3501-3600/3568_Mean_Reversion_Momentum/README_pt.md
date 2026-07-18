# Estratégia de Momento de Reversão à Média
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Mean Reversion é uma porta direta do consultor especialista MetaTrader *Mean reversion.mq4*. A versão StockSharp mantém a ideia comercial original: comprar após uma longa série de fechamentos decrescentes e vender após uma corrida de alta semelhante. As entradas são confirmadas pelo alinhamento de tendências usando duas médias móveis lineares ponderadas, força de impulso em um período de tempo mais alto e um filtro mensal MACD.

Uma vez posicionada, a estratégia recria as regras de gerenciamento de dinheiro da versão MQL: stop-loss e take-profit configuráveis em pips, realocação opcional do ponto de equilíbrio e um trailing stop que bloqueia os lucros à medida que o mercado se move a favor da negociação.

## Lógica de negociação
1. **Período de sinal** – a estratégia opera na série de velas selecionada (padrão 15 minutos).
2. **Detecção de exaustão** – coleta os últimos `BarsToCount` fechamentos. Uma configuração longa exige que o fechamento mais recente seja inferior a cada um dos fechamentos anteriores, sinalizando uma liquidação. Uma configuração curta precisa da condição oposta.
3. **Filtro de tendência** – LWMA rápido (comprimento `FastMaLength`) deve estar acima do LWMA lento (`SlowMaLength`) para posições compradas e abaixo para posições vendidas.
4. **Filtro de impulso** – o indicador de impulso (período `MomentumLength`) é calculado no período de tempo superior do estilo MetaTrader (M15 → H1, H1 → D1, etc.). Pelo menos uma das últimas três leituras de momento deve divergir de 100 em mais de `MomentumThreshold`.
5. **MACD confirmação** – um MACD mensal (26/12/9) deve ter a linha principal acima da linha de sinal para posições compradas e abaixo para posições vendidas.

Se todas as condições forem satisfeitas, a estratégia abre uma posição usando `OrderVolume`. As negociações opostas achatam a posição atual antes de reverter.

## Gerenciamento de posição
- **Stop-loss e take-profit** – configurados em pips via `StopLossPips` e `TakeProfitPips`.
- **Ponto de equilíbrio** – quando ativado, o stop é movido para o preço de entrada mais `BreakEvenOffsetPips` após o preço avançar em `BreakEvenTriggerPips`.
- **Trailing Stop** – se `EnableTrailing` for verdadeiro e o lucro não realizado exceder `TrailingStopPips`, o stop segue o preço com a etapa `TrailingStepPips`.

Todas as conversões de preço usam o tamanho do pip do instrumento para corresponder ao comportamento MetaTrader.

## Parâmetros
| Nome | Descrição | Padrão |
|------|-------------|---------|
| `OrderVolume` | Tamanho do pedido usado para entradas no mercado. | `1` |
| `CandleType` | Série de velas primárias usadas para sinais. | `M15` |
| `BarsToCount` | Número de fechamentos anteriores verificados quanto à exaustão. | `10` |
| `FastMaLength` | Período LWMA rápido. | `6` |
| `SlowMaLength` | Período LWMA lento. | `85` |
| `MomentumLength` | Período de impulso no período de tempo superior. | `14` |
| `MomentumThreshold` | Desvio absoluto mínimo de 100 para confirmação do impulso. | `0.3` |
| `StopLossPips` | Distância de stop-loss em pips. | `20` |
| `TakeProfitPips` | Distância de lucro em pips. | `50` |
| `UseBreakEven` | Habilite a relocação de parada para atingir o ponto de equilíbrio. | `false` |
| `BreakEvenTriggerPips` | Lucro em pips necessário antes de mover o stop. | `30` |
| `BreakEvenOffsetPips` | Pips extras adicionados ao passar para o ponto de equilíbrio. | `30` |
| `EnableTrailing` | Ative o gerenciamento de trailing stop. | `true` |
| `TrailingStopPips` | Lucro em pips necessário para iniciar o trailing. | `40` |
| `TrailingStepPips` | Distância mantida pelo trailing stop. | `40` |

## Notas
- O prazo mais alto para o impulso segue MetaTrader etapas: M1→M15, M5→M30, M15→H1, M30→H4, H1→D1, H4→W1, D1→MN1, W1→MN1.
- A confirmação de MACD sempre usa o período mensal (MN1).
- A estratégia espera tipos de velas baseadas em prazos; velas de tick ou intervalo não são suportadas.
