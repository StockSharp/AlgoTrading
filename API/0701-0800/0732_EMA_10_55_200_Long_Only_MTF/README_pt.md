# Estratégia EMA 10/55/200 Somente Comprado MTF
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia abre posições compradas quando os cruzamentos de EMA no gráfico de 4 horas se alinham com tendências de alta nos gráficos diários e semanais.

## Detalhes

- **Critérios de entrada**:
  - `EMA10` cruza acima de `EMA55` com a máxima da vela acima de `EMA55`, ou `EMA55` cruza acima de `EMA200`, ou `EMA10` cruza acima de `EMA500`.
  - A `EMA55` diária está acima de `EMA200` e a `EMA55` semanal está acima de `EMA200`.
- **Critérios de saída**:
  - `EMA10` cruza abaixo de `EMA200` ou `EMA500`.
  - O preço cai ao nível de stop loss.
- **Parâmetros**:
  - `EMA 10 Length` = 10
  - `EMA 55 Length` = 55
  - `EMA 200 Length` = 200
  - `EMA 500 Length` = 500
  - `Stop Loss %` = 5
