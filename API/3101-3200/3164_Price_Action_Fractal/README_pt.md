# Estratégia de Price Action Fractal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é um port em C# do assessor especialista "PRICE_ACTION" do MetaTrader. Combina fractais de Williams com médias móveis ponderadas, filtros de Momentum e MACD para negociar rompimentos confirmados pela ação do preço no período selecionado.

## Ideia

1. Analisar apenas velas concluídas; cada decisão é tomada no fechamento da barra do período configurado.
2. Detectar novos fractais altistas ou baixistas usando uma janela de 5 velas. Um novo fractal de baixa sinaliza suporte potencial, enquanto um fractal de alta sinaliza resistência potencial.
3. Confirmar o viés direcional com duas médias móveis ponderadas lineares (LWMA). Operações compradas requerem a LWMA rápida acima da lenta; vendidas requerem o oposto.
4. Validar o Momentum verificando o desvio absoluto do indicador Momentum do nível neutro de 100 no período superior.
5. Usar um filtro MACD (12,26,9 por padrão): setups altistas exigem MACD acima de sua linha de sinal, setups baixistas exigem MACD abaixo da linha de sinal.
6. Quando todos os filtros concordam, entrar na direção do rompimento e gerenciar a posição com stops fixos, um Trailing stop e um deslocamento de break-even opcional.

## Regras de entrada

- **Entrada comprada**
  - Um novo fractal de baixa se forma na vela atual (padrão de cinco barras).
  - Fast LWMA &gt; Slow LWMA.
  - `abs(Momentum - 100)` &ge; `MomentumThreshold`.
  - Linha principal MACD &gt; linha de sinal MACD.
  - O tamanho da posição é baseado no volume da estratégia e limitado por `MaxPositionUnits`.

- **Entrada vendida**
  - Um novo fractal de alta se forma na vela atual.
  - Fast LWMA &lt; Slow LWMA.
  - `abs(Momentum - 100)` &ge; `MomentumThreshold`.
  - Linha principal MACD &lt; linha de sinal MACD.

## Regras de saída

- Stop-loss fixo (`StopLossPoints`) e take-profit fixo (`TakeProfitPoints`) expressos em passos de preço.
- Trailing stop opcional (`TrailingStopPoints`) que segue o preço mais favorável quando a posição ganha pelo menos a distância de trailing.
- Proteção de break-even opcional: após atingir `BreakEvenTriggerPoints` o stop é deslocado para `EntryPrice ± BreakEvenOffsetPoints`.
- Saídas são executadas com ordens de mercado; todos os cálculos dependem de máximas/mínimas de velas para detectar acionamentos do stop.

## Gerenciamento de posição

- A estratégia mantém uma única posição agregada por símbolo.
- `Volume` define o tamanho base da ordem. Ao reverter, a estratégia primeiro fecha a exposição oposta e então abre uma nova posição com o tamanho solicitado.
- `MaxPositionUnits` limita o valor absoluto da posição para evitar superdimensionamento.

## Parâmetros

- `CandleType` – período usado para cada indicador e decisão (equivalente à variável MQL `T`).
- `FastMaPeriod` / `SlowMaPeriod` – comprimentos das médias móveis ponderadas (`FastMA`, `SlowMA`).
- `MomentumPeriod` – comprimento de retrovisão do Momentum (fixado em 14 no script MQL).
- `MomentumThreshold` – desvio absoluto mínimo de 100 necessário para confirmar o Momentum (`Mom_Buy`/`Mom_Sell`).
- `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` – configuração do MACD (12/26/9 por padrão).
- `StopLossPoints`, `TakeProfitPoints` – distâncias em passos de preço para ordens protetoras (`Stop_Loss`, `Take_Profit`).
- `TrailingStopPoints` – distância do Trailing stop (`TrailingStop`).
- `BreakEvenTriggerPoints`, `BreakEvenOffsetPoints` – ativador e deslocamento de break-even (`WHENTOMOVETOBE`, `PIPSTOMOVESL`).
- `FractalLifetime` – número de velas que um fractal detectado permanece válido (`CandlesToRetrace`).
- `MaxPositionUnits` – tamanho máximo absoluto de posição (restrição `Max_Trades` em unidades de lote).
- `EnableTrailing`, `EnableBreakEven`, `UseStopLoss`, `UseTakeProfit` – interruptores para os mecanismos de saída respectivos.

## Diferenças em relação ao EA original

- Recursos a nível de portfólio como take-profit baseado em dinheiro, stop de patrimônio e alertas por e-mail/notificação não estão implementados.
- As rotinas de otimização de lotes do MetaTrader são simplificadas; a estratégia usa a normalização de volume do StockSharp.
- As ordens protetoras são executadas com saídas de mercado em vez de modificações de ordens pendentes porque o StockSharp lida com o gerenciamento de risco de forma diferente.

## Arquivos

- `CS/PriceActionFractalStrategy.cs` – implementação da estratégia em C#.
- A versão em Python ainda não está disponível.
