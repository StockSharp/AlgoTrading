# Estratégia de Ação Volátil
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina um rompimento de volatilidade de curto prazo com o filtro de tendência **Alligator** de Bill Williams calculado no período de 4 horas.

## Regras de negociação
- **Entrada comprada** quando:
  - O ATR de período 1 é maior que *Volatility Coef* vezes o ATR com período *ATR Period*.
  - A vela é de alta e estabelece uma nova máxima de 24 barras.
  - As linhas do Alligator estão alinhadas para cima (Lips > Teeth > Jaw) e tanto a abertura quanto o fechamento estão acima da linha Teeth.
- **Entrada vendida** quando as condições acima se espelham na direção oposta.

Na entrada, a estratégia define níveis de stop-loss e take-profit como múltiplos do ATR(1):
- Stop-loss = preço de entrada ± *Stop Coef* × ATR(1)
- Take-profit = preço de entrada ± *Profit Coef* × ATR(1)

## Parâmetros
- **Volatility Coef** – multiplicador comparando ATR rápido com ATR lento.
- **ATR Period** – período do ATR lento.
- **Stop Coef** – multiplicador ATR para stop-loss.
- **Profit Coef** – multiplicador ATR para take-profit.
- **Candle Type** – período para a análise principal (o Alligator usa velas de 4H).
