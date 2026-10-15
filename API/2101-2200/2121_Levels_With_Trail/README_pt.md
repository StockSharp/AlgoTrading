# Estratégia de Níveis com Trailing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Convertida do script MQL `levels_with_trail.mq4`. A estratégia abre negociações quando o preço cruza um nível especificado e pode aplicar trailing ao stop-loss.

## Como funciona
- Subscreve velas do período escolhido.
- Quando não há posição aberta e o preço de fechamento está acima de `Level Price`, compra; se o preço estiver abaixo, vende.
- Se `Trail Stop` estiver ativado, o stop-loss segue o preço quando a posição é lucrativa.
- As posições são fechadas quando o stop-loss, o take-profit ou um sinal de rompimento oposto são ativados.

## Parâmetros
- `Stop Loss` – tamanho do stop-loss em unidades de preço.
- `Take Profit` – tamanho do take-profit em unidades de preço.
- `Level Price` – nível de rompimento a ser monitorado.
- `Trail Stop` – ativar ou desativar o trailing stop-loss.
- `Candle Type` – período de velas utilizado para análise.
