# Estratégia Martingala com Condições de Abertura MACD e KDJ
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia entra em negociações quando tanto a linha MACD quanto a linha %K do KDJ cruzam suas linhas de sinal na mesma direção. Piramida posições usando uma abordagem martingala, adicionando quando o preço se move contra a negociação por uma porcentagem configurada e então reverte.

As posições são fechadas quando uma condição de take profit, stop loss ou trailing stop é atingida.

## Detalhes

- **Entrada**: A linha MACD e a linha %K do KDJ cruzam suas linhas de sinal na mesma direção.
- **Adições**: Até `Max Additions` vezes quando o preço se move `Add Position Percent` e reverte `Rebound Percent`. O tamanho de cada adição é multiplicado por `Add Multiplier`.
- **Saída**: Fechar em `Take Profit Trigger`, `Stop Loss Percent` ou ao acionar o trailing stop.
- **Direção**: Comprado e vendido.

