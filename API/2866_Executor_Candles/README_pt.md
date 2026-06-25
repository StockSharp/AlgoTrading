# Estratégia de Velas Executoras
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma conversão direta do especialista MetaTrader "Executor Candles". Reage a um rico conjunto de padrões de reversão altistas e baixistas de velas e pode opcionalmente confirmar operações com uma vela de tendência de timeframe superior. Toda a lógica de gestão de operações – stops, take-profits e trailing stops – espelha o comportamento do especialista original medido em pips (passos de preço).

## Como funciona

- **Filtro de tendência**: Quando `UseTrendFilter` está habilitado, a estratégia observa a vela terminada mais recente de `TrendCandleType`. Configurações compradas são permitidas apenas se essa vela fechou altista, enquanto configurações vendidas exigem um fechamento baixista. Com o filtro desativado (padrão), apenas a lógica de padrões é usada.
- **Padrões comprados**: Martelo, engolfo altista, linha de penetração, estrela da manhã e estruturas de estrela doji da manhã tomadas das últimas três velas de trading completadas.
- **Padrões vendidos**: Enforcado, engolfo baixista, cobertura de nuvem escura, estrela vespertina e confirmações de estrela doji vespertina.
- **Gestão de operações**:
  - Distâncias separadas de stop-loss e take-profit para posições compradas e vendidas expressas em pips (`StopLossBuyPips`, `TakeProfitBuyPips`, `StopLossSellPips`, `TakeProfitSellPips`).
  - Trailing stops opcionais para ambas as direções controlados por `TrailingStopBuyPips`, `TrailingStopSellPips` e o deslocamento mínimo `TrailingStepPips`. Uma atualização de trailing é feita somente após o preço avançar pela distância do stop mais o passo de trailing, replicando a lógica do MetaTrader.
  - As ordens são colocadas com `OrderVolume` lotes e a posição atual é totalmente revertida com ordens de mercado quando uma condição de saída é acionada.

A estratégia assina o `CandleType` configurado para sinais de trading e, se necessário, o `TrendCandleType` para a vela de confirmação. Mantém um buffer interno das últimas três velas de trading terminadas para avaliar os padrões de múltiplas barras sem armazenar longos históricos.

## Parâmetros

- `CandleType` – timeframe usado para detectar os padrões de velas.
- `TrendCandleType` – vela de timeframe superior usada quando o filtro de tendência está ativo.
- `OrderVolume` – tamanho de ordem para entradas e saídas de mercado.
- `StopLossBuyPips`, `TakeProfitBuyPips`, `TrailingStopBuyPips` – controles de risco para posições compradas.
- `StopLossSellPips`, `TakeProfitSellPips`, `TrailingStopSellPips` – controles de risco para posições vendidas.
- `TrailingStepPips` – movimento favorável mínimo antes de o trailing stop ser ajustado.
- `UseTrendFilter` – habilita ou desabilita a confirmação de timeframe superior.

## Notas

- Todas as distâncias baseadas em pips são multiplicadas pelo `PriceStep` do instrumento. Garanta que esteja corretamente configurado para níveis de risco precisos.
- As verificações de entrada são executadas em cada vela terminada; os ticks ao vivo simplesmente atualizam a barra mais recente sem alterar o fluxo de decisões.
- A estratégia emite apenas ordens de mercado e espera que a execução ocorra imediatamente como na versão do MetaTrader.
