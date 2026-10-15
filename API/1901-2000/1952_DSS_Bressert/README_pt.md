# Estratégia DSS Bressert
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa o indicador Double Smoothed Stochastic (DSS) Bressert. Duas linhas são calculadas:

- **Linha DSS** – valor estocástico suavizado duas vezes com média móvel exponencial.
- **Linha MIT** – valor intermediário após o primeiro suavizado.

Uma negociação é aberta quando essas linhas se cruzam:

- Comprar quando a linha DSS cruza abaixo da linha MIT após ter estado acima.
- Vender quando a linha MIT cruza abaixo da linha DSS após ter estado acima.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `EmaPeriod` | Período de suavização EMA (padrão: 8) |
| `StoPeriod` | Período de cálculo estocástico (padrão: 13) |
| `TakeProfitPercent` | Percentual de take profit para ordens de proteção (padrão: 2) |
| `StopLossPercent` | Percentual de stop loss para ordens de proteção (padrão: 1) |
| `CandleType` | Período de tempo usado para os cálculos (padrão: 4 horas) |

## Observações

- A estratégia funciona apenas em candles fechados.
- A proteção usa stop loss e take profit baseados em percentual.
