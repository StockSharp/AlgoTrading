# Estratégia Exp Martin V2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Exp Martin V2 implementa uma abordagem martingale exponencial. Ela sempre mantém uma única posição aberta e, após cada negociação, decide a próxima direção e volume com base no lucro da última transação.

A estratégia começa com um tipo de ordem predefinido (compra ou venda) e um volume inicial. Um take-profit e stop-loss fixos são aplicados a cada posição. Quando uma negociação é fechada com lucro, uma nova posição do mesmo tipo e volume inicial é aberta. Se a negociação terminar com perda, a direção é invertida e o volume é multiplicado por um fator especificado. A multiplicação continua após cada perda até que um número máximo de multiplicações seja alcançado; então o volume é redefinido para o valor inicial.

Isso cria uma sequência escalonada de trades opostos que visa recuperar as perdas anteriores assim que ocorrer um movimento rentável.

## Detalhes

- **Lógica de entrada**:
  - Abrir a posição inicial de acordo com *Start Type* (0 - compra, 1 - venda) com o *Start Volume*.
  - Após um trade lucrativo, repetir a mesma direção com o volume inicial.
  - Após um trade com perda, inverter a direção e multiplicar o volume por *Factor* até atingir as multiplicações de *Limit*.
- **Comprado/Vendido**: Ambos, dependendo da sequência atual.
- **Lógica de saída**:
  - As posições são fechadas quando o preço atinge os níveis configurados de *Take Profit* ou *Stop Loss*.
- **Stops**: Stop-loss e take-profit fixos em pontos.
- **Filtros**: Nenhum.
- **Gestão de posição**: Apenas uma posição aberta por vez.

Use esta estratégia para experimentar gestão de dinheiro martingale no StockSharp sem indicadores adicionais.
