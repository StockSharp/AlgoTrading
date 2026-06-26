# Estratégia de Flat Channel Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Flat Channel Strategy** é uma tradução em C# do assessor especialista MetaTrader 5 *Flat Channel (edição de barabashkakvn)*. Mantém o fluxo de trabalho original: um desvio padrão suavizado destaca compressões de volatilidade, os preços mais altos e mais baixos dentro da compressão definem um canal horizontal, e ordens de stop pendentes são colocadas logo fora desse intervalo. Quando o mercado rompe, a estratégia acompanha o movimento com níveis de stop-loss e take-profit predefinidos e pode opcionalmente trailear o stop à medida que a posição ganha lucro.

## Como funciona

1. **Detecção de compressão de volatilidade** – Um indicador `StandardDeviation` com comprimento `StdDevPeriod` é suavizado por uma curta `SimpleMovingAverage` de `SmoothingLength`. Sempre que a série suavizada imprime `FlatBars` valores consecutivos não crescentes, o mercado é tratado como flat e as flags das ordens são rearmadas.
2. **Construção do canal** – Uma vez confirmado um flat, a estratégia solicita a máxima mais alta e a mínima mais baixa nas últimas `max(ChannelLookback, FlatBars + 1)` velas usando os indicadores integrados `Highest`/`Lowest`. A altura do canal é filtrada por `ChannelMinPips`/`ChannelMaxPips` após converter pips em unidades de preço através de `PipSize` (ou o tamanho do tick detectado quando o parâmetro é deixado em zero).
3. **Ordens pendentes** – Se a posição atual é flat e o trading é permitido, a estratégia submete uma ordem de compra stop em `high + IndentPips` e uma ordem de venda stop em `low − IndentPips`. Cada ordem lembra os níveis protetores calculados no momento do envio.
4. **Execução do Rompimento** – Quando uma ordem pendente é preenchida, a ordem pendente oposta é cancelada automaticamente. O preço preenchido torna-se a âncora de entrada para a lógica de trailing stop e as distâncias de stop-loss/take-profit memorizadas são ativadas.
5. **Gestão de posição** – A posição ativa é supervisionada em cada vela completada. Se o preço tocar o nível de stop-loss ou take-profit, a estratégia emite uma saída de mercado. Quando `TrailingStopPips` é maior que zero, o stop é avançado uma vez que o preço de fechamento se move pelo menos `TrailingStopPips + TrailingStepPips` a partir do preço de preenchimento.
6. **Filtro de sessão** – Quando `UseTradingHours` está habilitado, a lógica de rompimento só funciona entre `StartHour` (inclusive) e `EndHour` (exclusivo). Sessões noturnas são suportadas permitindo `StartHour > EndHour`.

## Gestão de risco

- **Proteção dinâmica ou fixa** – Configure `StopLossPips` / `TakeProfitPips` em valores positivos para usar distâncias fixas (em pips). Mantê-los em zero muda para dimensionamento dinâmico baseado na altura do canal e nos coeficientes `DynamicStopMultiplier` / `DynamicTakeMultiplier`.
- **Trailing stop** – Habilite `TrailingStopPips` para seguir o movimento assim que a negociação estiver no lucro. A lógica de trailing respeita `TrailingStepPips` para evitar microajustes.
- **Limite de posição** – `MaxPositions` limita a exposição agregada a `MaxPositions × TradeVolume`. Se esse limiar for atingido, novas ordens pendentes não são submetidas até que a exposição diminua.
- **Filtros direcionais** – `UseBuy` e `UseSell` permitem que a estratégia opere em modos apenas-Rompimento, apenas-Rompimento descendente ou bidirecional.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `TradeVolume` | `1` | Volume submetido com cada ordem pendente. |
| `PipSize` | `0.0001` | Substituição manual do tamanho do pip. Deixar em zero para usar o tamanho do tick do instrumento (com ajuste automático de 3/5 dígitos). |
| `StdDevPeriod` | `46` | Lookback para o `StandardDeviation` base. |
| `SmoothingLength` | `3` | Comprimento da média móvel aplicado à série de volatilidade. |
| `FlatBars` | `3` | Número de valores de volatilidade suavizada consecutivos não crescentes necessários para rearmar as ordens de rompimento. |
| `ChannelLookback` | `5` | Velas usadas para medir a máxima mais alta e a mínima mais baixa após um flat ser detectado. Comparado automaticamente com `FlatBars + 1`. |
| `ChannelMinPips` | `15` | Altura mínima do canal (em pips). Defina como `0` para desabilitar o limite inferior. |
| `ChannelMaxPips` | `105` | Altura máxima do canal (em pips). Defina como `0` para desabilitar o limite superior. |
| `DynamicStopMultiplier` | `1` | Multiplicador de altura do canal para cálculo dinâmico de stop-loss quando `StopLossPips = 0`. |
| `DynamicTakeMultiplier` | `1` | Multiplicador de altura do canal para cálculo dinâmico de take-profit quando `TakeProfitPips = 0`. |
| `StopLossPips` | `0` | Distância fixa de stop-loss em pips. Substitui a fórmula dinâmica quando positivo. |
| `TakeProfitPips` | `0` | Distância fixa de take-profit em pips. Substitui a fórmula dinâmica quando positivo. |
| `IndentPips` | `0` | Deslocamento adicional (em pips) além dos limites do canal para ordens pendentes. |
| `TrailingStopPips` | `5` | Distância do trailing stop em pips. Defina como `0` para desabilitar o trailing. |
| `TrailingStepPips` | `5` | Passo mínimo (em pips) necessário para mover o trailing stop. |
| `UseBuy` | `true` | Habilitar rompimentos longos (ordem de compra stop). |
| `UseSell` | `true` | Habilitar rompimentos curtos (ordem de venda stop). |
| `MaxPositions` | `5` | Número máximo de volumes base permitidos na posição agregada. |
| `UseTradingHours` | `true` | Habilitar o filtro de sessão de trading. |
| `StartHour` | `0` | Hora de início da sessão (inclusive). |
| `EndHour` | `23` | Hora de fim da sessão (exclusivo). |
| `CandleType` | `H1` | Série de velas usada para cálculos (padrão: período de 1 hora). |

## Notas

- A estratégia opera exclusivamente em velas completadas através da API de alto nível `SubscribeCandles().Bind(...)`, correspondendo ao comportamento determinista esperado do EA original.
- Os preços protetores são normalizados através de `Security.ShrinkPrice` para respeitar os tamanhos de tick da bolsa.
- Quando ambas as ordens pendentes estão ativas e uma delas é preenchida, a ordem oposta é cancelada imediatamente para que apenas uma posição de rompimento possa estar aberta de cada vez.
