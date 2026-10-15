# Estratégia Broadening Top
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Broadening Top Strategy é um sistema seguidor de tendência inspirado no expert advisor original "Broadening top" do MetaTrader. A estratégia se concentra em capturar rompimentos que aparecem após uma formação de alargamento, combinando direção de tendência e confirmação de momentum. Duas médias móveis linearmente ponderadas, um oscilador de momentum e um filtro MACD trabalham juntos para detectar rompimentos altistas e baixistas.

## Lógica de negociação
1. **Filtro de tendência:** a estratégia compara uma média móvel linearmente ponderada (LWMA) rápida e uma lenta. Operações compradas exigem que a LWMA rápida esteja acima da lenta, enquanto operações vendidas esperam o oposto.
2. **Confirmação de momentum:** o oscilador de momentum é observado nos três últimos candles concluídos. Uma operação só é permitida se qualquer um desses valores se desviar do nível neutro (100) pelo menos pelo limiar configurado (valores separados para compras e vendas).
3. **Alinhamento MACD:** um filtro adicional verifica a linha MACD contra sua linha de sinal. Posições compradas são disparadas apenas quando a linha MACD está acima da linha de sinal; vendas, quando está abaixo.
4. **Tratamento de posição:** antes de abrir uma operação na direção oposta, a estratégia fecha a posição atual, garantindo que apenas uma posição esteja ativa por vez.

## Gestão de risco
A estratégia usa `StartProtection` para gerenciar ordens protetoras:
- Distâncias opcionais de stop-loss e take-profit definidas em passos de preço (pips).
- Um trailing stop opcional com passo de trailing configurável.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `OrderVolume` | Tamanho da ordem em lotes/contratos. | 1 |
| `FastMaLength` | Comprimento da média móvel linearmente ponderada rápida. | 6 |
| `SlowMaLength` | Comprimento da média móvel linearmente ponderada lenta. | 85 |
| `MomentumPeriod` | Período de retrospectiva do oscilador de momentum. | 14 |
| `MomentumBuyThreshold` | Distância mínima do nível neutro de momentum (100) exigida para permitir entradas compradas. | 0.3 |
| `MomentumSellThreshold` | Distância mínima do nível neutro de momentum (100) exigida para permitir entradas vendidas. | 0.3 |
| `MacdFast` | Comprimento da EMA rápida dentro do MACD. | 12 |
| `MacdSlow` | Comprimento da EMA lenta dentro do MACD. | 26 |
| `MacdSignal` | EMA de sinal dentro do MACD. | 9 |
| `TakeProfitPips` | Distância do take-profit medida em passos de preço. | 50 |
| `StopLossPips` | Distância do stop-loss medida em passos de preço. | 20 |
| `TrailingStopPips` | Distância do trailing-stop medida em passos de preço. | 40 |
| `TrailingStepPips` | Distância adicional antes da atualização do trailing stop. | 10 |
| `CandleType` | Tipo de candle/timeframe usado para cálculos. | Timeframe de 15 minutos |
| `EnableLongs` | Habilita ou desabilita operações compradas. | true |
| `EnableShorts` | Habilita ou desabilita operações vendidas. | true |

## Indicadores
- **LinearWeightedMovingAverage:** filtros de tendência rápido e lento.
- **Momentum:** confirma aceleração do mercado para longe do nível neutro.
- **MovingAverageConvergenceDivergenceSignal:** fornece confirmação direcional via linhas MACD e de sinal.

## Notas de uso
- Limiares de momentum são avaliados nos três candles concluídos mais recentes para emular o comportamento MQL original.
- Ordens protetoras (stop-loss, take-profit, trailing stop) são opcionais e podem ser desabilitadas definindo a distância correspondente como zero.
- A estratégia deve ser anexada a ativos que forneçam passo de preço e informações decimais para calcular corretamente o tamanho do pip.
