# Estratégia de JBrainTrend1Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia de JBrainTrend1Stop** é um port StockSharp do consultor especialista MetaTrader 5 `Exp_JBrainTrend1Stop`. Combina duas medidas de Average True Range, um oscilador Stochastic e médias móveis Jurik para detectar reversões de tendência BrainTrading. Quando o preço suavizado por Jurik faz um swing suficientemente grande e o Stochastic sai de sua zona neutra, a estratégia muda o viés, atualiza a linha de stop BrainTrend e (opcionalmente) inverte a posição líquida após um atraso configurável.

## Lógica de trading

1. Subscrever velas definidas por `CandleType` e alimentá-las a:
   - Um `AverageTrueRange` primário com comprimento `AtrPeriod`.
   - Um `AverageTrueRange` estendido com período `AtrPeriod + StopDPeriod`.
   - Um `StochasticOscillator` com `StochasticPeriod` e suavização de %K de uma barra (para corresponder às configurações MT5).
   - Três instâncias de `JurikMovingAverage` (máxima, mínima e fechamento) configuradas com `JmaLength` e `JmaPhase`.
2. Para cada vela concluída calcular:
   - `range = ATR / 2.3` (correspondendo à constante original `d = 2.3`).
   - `range1 = ATR_extended * 1.5` (correspondendo a `s = 1.5`).
   - `val3 = |JMA_close - JMA_close[shift 2]|` que reproduz a diferença do buffer MT5.
3. Quando `val3 > range` e o Stochastic sai de sua banda neutra:
   - Se `%K < 47` a estratégia entra no estado BrainTrend baixista (`_trendState = -1`), inicializa o stop de venda em `JMA_high + range1 / 4` e gera um sinal de **venda**.
   - Se `%K > 53` a estratégia entra no estado altista (`_trendState = 1`), inicializa o stop de compra em `JMA_low - range1 / 4` e gera um sinal de **compra**.
4. Enquanto o estado permanece inalterado, o stop BrainTrend é arrastado em direção ao preço por `range1` (`JMA_high + range1` para tendências baixistas, `JMA_low - range1` para tendências altistas).
5. Os sinais são liberados após `SignalBar` barras concluídas. Na execução:
   - Um sinal de compra fecha posições vendidas (se `SellClose` estiver habilitado) e opcionalmente abre uma nova posição comprada (se `BuyOpen` estiver habilitado).
   - Um sinal de venda fecha posições compradas (se `BuyClose` estiver habilitado) e opcionalmente abre uma nova posição vendida (se `SellOpen` estiver habilitado).

Os gráficos exibem automaticamente o fechamento suavizado por Jurik e o oscilador Stochastic junto com marcadores de operações.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `CandleType` | Série de velas processada pela estratégia. | H4 (período de 4 horas) |
| `AtrPeriod` | Comprimento do ATR primário para o gatilho BrainTrend. | 7 |
| `StochasticPeriod` | Período para %K/%D do oscilador Stochastic (suavização de %K de uma barra). | 9 |
| `StopDPeriod` | Barras adicionais ao período ATR secundário (`AtrPeriod + StopDPeriod`). | 3 |
| `JmaLength` | Comprimento da média móvel Jurik aplicada a máxima/mínima/fechamento. | 7 |
| `JmaPhase` | Argumento de fase enviado às médias móveis Jurik (limitado a [-100; 100]). | 100 |
| `SignalBar` | Número de barras concluídas a aguardar antes de disparar um novo sinal. | 1 |
| `BuyOpen` / `SellOpen` | Permitir entrar em posições compradas/vendidas após um sinal. | `true` |
| `BuyClose` / `SellClose` | Permitir fechar posições compradas/vendidas existentes num sinal oposto. | `true` |

Usar a propriedade `Volume` da estratégia ou a configuração do broker para controlar o tamanho da ordem.

## Diferenças em relação à versão MT5

- O bloco de gerenciamento de dinheiro original (`MM`, `MMMode`, `Deviation_`, dimensionamento dinâmico de lotes) é substituído pelo dimensionamento padrão de ordens do StockSharp via `Volume` e ordens de mercado. O controle de slippage não é reproduzido.
- As distâncias absolutas de stop-loss e take-profit (`StopLoss_`, `TakeProfit_`) não estão implementadas. A proteção pode ser configurada manualmente através do ambiente de hosting se necessário.
- Os níveis de stop BrainTrend são usados internamente para o timing do sinal; não são colocados como ordens pendentes.
- As médias móveis Jurik dependem da implementação `JurikMovingAverage` do StockSharp. O parâmetro de fase é aplicado por reflexão, correspondendo ao comportamento de outros ports BrainTrading neste repositório.

## Uso

1. Anexar a estratégia a um ativo e definir `CandleType` (ex.: velas de 4 horas para consistência com o EA).
2. Ajustar os parâmetros do indicador (`AtrPeriod`, `StochasticPeriod`, `StopDPeriod`, `JmaLength`, `JmaPhase`) para alinhar com a sensibilidade BrainTrend desejada.
3. Ajustar `SignalBar` para atrasar a execução de sinais por várias barras concluídas se necessário.
4. Configurar `Volume` e os toggles de abertura/fechamento para refletir a direção de trading preferida.
5. (Opcional) Adicionar gerenciamento de risco externo como stop-loss ou limites de portfólio via a plataforma de hosting.

Uma vez em execução, a estratégia rastreará reversões BrainTrend, fechará posições opostas e opcionalmente mudará a direção após o atraso configurado.
