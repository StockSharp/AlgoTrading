# Estratégia Exp MAMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera utilizando o indicador MESA Adaptive Moving Average (MAMA).

O indicador produz duas linhas:

- **MAMA** – a média móvel adaptativa.
- **FAMA** – uma média de acompanhamento utilizada como linha de sinal.

Lógica de operação:

1. Quando MAMA cruza abaixo de FAMA, a estratégia fecha posições vendidas e abre uma nova posição comprada.
2. Quando MAMA cruza acima de FAMA, a estratégia fecha posições compradas e abre uma nova posição vendida.

## Parâmetros

- `FastLimit` – limite alfa rápido utilizado pelo fator adaptativo.
- `SlowLimit` – limite alfa lento utilizado pelo fator adaptativo.
- `CandleType` – período para as velas recebidas.
- `BuyOpen` / `SellOpen` – permitem abrir posições compradas ou vendidas.
- `BuyClose` / `SellClose` – permitem fechar posições compradas ou vendidas.

A estratégia opera em velas finalizadas e utiliza ordens a mercado para entrada e saída.
