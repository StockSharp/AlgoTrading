# Estratégia DigVariation
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é inspirada no exemplo MQL5 *DigVariation*. Aproxima o indicador usando uma média móvel simples (SMA) e abre operações quando a direção da SMA muda.

## Lógica
- A SMA é calculada sobre as velas recebidas.
- Se os valores anteriores da SMA mostram uma inclinação ascendente e o valor mais recente continua mais alto, a estratégia abre uma posição comprada.
- Se os valores anteriores da SMA mostram uma inclinação descendente e o valor mais recente continua mais baixo, a estratégia abre uma posição vendida.
- As posições existentes são fechadas quando a tendência se reverte.

## Parâmetros
- **Period** – período de cálculo da SMA.
- **BuyOpen** – habilitar entradas compradas.
- **SellOpen** – habilitar entradas vendidas.
- **BuyClose** – permitir fechar posições compradas.
- **SellClose** – permitir fechar posições vendidas.
- **StopLoss** – valor de proteção contra perdas (passado para `StartProtection`).
- **TakeProfit** – valor alvo de lucro (passado para `StartProtection`).

## Notas
Esta é uma conversão simplificada. Utiliza uma SMA padrão em vez do indicador DigVariation personalizado original.
