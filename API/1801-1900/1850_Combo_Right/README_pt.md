# Estratégia Combo Right
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Implementação da estratégia convertida do script MQL `combo_right.mq5`.
O sistema combina um sinal básico de CCI com três perceptrons simples que trabalham sobre diferenças de preços.

## Lógica

1. **Sinal básico** – valor do Commodity Channel Index (CCI). Valores positivos favorecem operações compradas, valores negativos favorecem operações vendidas.
2. **Perceptrons** – cada perceptron analisa um conjunto de preços de fechamento deslocados e aplica pesos lineares. O parâmetro de modo `Pass` seleciona quais perceptrons estão ativos:
   - `1`: apenas sinal básico de CCI.
   - `2`: perceptron de venda pode substituir o CCI e abrir posições vendidas.
   - `3`: perceptron de compra pode substituir o CCI e abrir posições compradas.
   - `4`: perceptron geral supervisiona tanto os perceptrons de compra quanto de venda.

Se um perceptron ativo emite um sinal, ele substitui a saída básica do CCI. Caso contrário, o valor do CCI é usado.

## Parâmetros

- `TakeProfit1`, `StopLoss1` – metas de lucro e perda para o sinal básico de CCI (em ticks).
- `CciPeriod` – período de lookback do indicador CCI.
- Pesos e períodos de cada perceptron (`x12`, `x22`, …, `p4`).
- `Pass` – modo de operação.
- `Shift` – índice de barra usado para dados de preços (0 atual, 1 anterior).
- `Volume` – volume de negociação.
- `CandleType` – tipo de candle para os cálculos.

## Indicadores

- CCI.
