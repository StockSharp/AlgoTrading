# Estratégia do Testador
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia é uma versão C# do consultor especialista MetaTrader **Testinator v1.30a**. Abre apenas posições longas e as gerencia como uma cesta. Cada nova compra é permitida somente quando um conjunto configurável de filtros técnicos retornar "verdadeiro" e o preço tiver avançado um número mínimo de pips. A lógica de saída espelha a lógica de entrada usando outra máscara de filtro. O EA original também dependia de medições diárias ATR para gerenciamento de risco, portanto, a porta assina velas diárias, além do período principal.

## Lógica de negociação

### Máscara de filtro de entrada (parâmetro `BuySequence`)

A máscara usa os nove bits inferiores. Um bit definido deve satisfazer o teste correspondente na vela finalizada anterior.

| Pouco | Condição |
| --- | --------- |
| 1 | EMA(12) está acima de SMA(14). |
| 2 | EMA(50) permanece abaixo dos mínimos das últimas três velas. |
| 4 | O mínimo anterior está abaixo da banda inferior Bollinger (20, 2). |
| 8 | ADX(14) está acima de -DI e +DI é mais forte que -DI. |
| 16 | Stochastic (16, 4, 8) tem %K acima de %D e %D acima de 80. |
| 32 | Williams %R(14) é maior que -20. |
| 64 | A linha MACD(12, 26, 9) está acima da linha de sinal. |
| 128 | Ichimoku mostra Senkou Span A acima do Span B, Tenkan acima de Kijun e a mínima anterior acima do Span A. |
| 256 | RSI (período `RsiEntryPeriod`) está acima de `RsiEntryLevel` e aumentando em relação ao valor anterior. |

### Sair da máscara de filtro (parâmetro `CloseBuySequence`)

| Pouco | Condição |
| --- | --------- |
| 1 | SMA(14) está acima de EMA(12). |
| 2 | EMA(50) está acima dos máximos das últimas três velas. |
| 4 | A máxima anterior está acima da banda de saída superior Bollinger (`BollingerCloseLength`, `BollingerCloseDeviation`). |
| 8 | -DI está acima de +DI. |
| 16 | Stochastic %D está abaixo de 80. |
| 32 | Williams %R(14) é menor que -80. |
| 64 | A linha MACD está abaixo da linha de sinal. |
| 128 | Ichimoku Senkou Span B está acima do Senkou Span A. |
| 256 | RSI (período `RsiClosePeriod`) está abaixo de `RsiCloseLevel`. |

Uma cesta é estendida somente se todos os bits de entrada ativos retornarem verdadeiros, o número de compras for inferior a `MaxBuys` e o último preço de preenchimento estiver a pelo menos `StepPips` de distância. O cesto é achatado sempre que a máscara de saída passa ou quando os níveis de proteção são acionados.

### Controle de sessão e gerenciamento de risco

* A negociação ocorre apenas entre `TradeStartHour` e `TradeStartHour + TradeDurationHours - 1` (horário da Europa Oriental). Se a janela estiver fechada e a cesta estiver com lucro, todas as compras serão fechadas.
* As distâncias de proteção de stop e take-profit são expressas em pips. Definir um valor para `-1` o desativa, enquanto `0` ativa o multiplicador ATR (`StopRatio`, `TakeRatio`).
* O trailing stop usa a mesma lógica ATR através de `StartTrailPips`, `TrailStepPips`, `StartTrailRatio` e `TrailStepRatio`.
* A estratégia calcula diariamente ATR(15) valores em velas D1 para manter o comportamento idêntico ao EA.

## Parâmetros

* `TradeVolume` – tamanho do lote (volume) para cada compra no mercado.
* `BuySequence` / `CloseBuySequence` – máscaras de bits que habilitam filtros de indicadores individuais.
* `MaxBuys` – número máximo de compras abertas tratadas como uma cesta.
* `StepPips` – progresso do preço mínimo (pips) antes de adicionar ao carrinho.
* `TradeStartHour`, `TradeDurationHours` – define a janela diária de negociação.
* `TakeProfitPips`, `StopLossPips` – níveis de proteção fixos (desativações negativas, zero muda para proporções ATR).
* `StartTrailPips`, `TrailStepPips` – distância inicial e passo final (negativo desativa, zero usa proporções ATR).
* `TakeRatio`, `StopRatio`, `StartTrailRatio`, `TrailStepRatio` – multiplicador ATRes usados quando o valor fixo é igual a zero.
* `RsiEntryLevel`, `RsiEntryPeriod` – RSI limite e período para a máscara de entrada.
* `RsiCloseLevel`, `RsiClosePeriod` – RSI limite e período para a máscara de saída.
* `BollingerCloseLength`, `BollingerCloseDeviation` – parâmetros das bandas de saída Bollinger.
* `CandleType` – prazo das velas de trabalho (as velas diárias são inscritas automaticamente para ATR).

## Notas

* A porta mantém o modelo de contabilidade de cesta do EA original: todas as ordens são de compra e apenas as ordens de mercado são usadas.
* A lógica armazena intencionalmente valores de indicadores anteriores para imitar as verificações "bar[1]" de MetaTrader.
* A estratégia ignora as entradas não utilizadas de EA (`TakeAsBasket`, `StopAsBasket`, etc.) porque elas não afetaram a lógica MQL.
