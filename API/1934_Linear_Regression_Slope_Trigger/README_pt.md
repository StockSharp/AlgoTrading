# Estratégia de Gatilho de Inclinação de Regressão Linear
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia usa um indicador de inclinação de regressão linear e uma linha de gatilho derivada para identificar mudanças de tendência. Uma posição comprada é aberta quando a linha de gatilho cruza acima da linha de inclinação, enquanto uma posição vendida é aberta quando a linha de gatilho cruza abaixo da linha de inclinação. As posições existentes são fechadas quando um sinal oposto aparece. A abordagem é inspirada na estratégia MQL5 original "Exp_LinearRegSlopeV2".

## Lógica do indicador
1. A **Inclinação de Regressão Linear** é calculada sobre os preços de fechamento das velas durante um período configurável.
2. Uma **linha de gatilho** é calculada como `2 * slope - slope[Shift]`, onde `slope[Shift]` é o valor de inclinação de algumas barras atrás.
3. Os cruzamentos entre a linha de gatilho e a linha de inclinação servem como sinais de trading.

## Regras de trading
- **Entrar Comprado:** O gatilho cruza acima da inclinação e operações vendidas são permitidas.
- **Entrar Vendido:** O gatilho cruza abaixo da inclinação e operações compradas são permitidas.
- **Sair Comprado:** A inclinação sobe acima do gatilho.
- **Sair Vendido:** O gatilho sobe acima da inclinação.

## Parâmetros
- `SlopeLength` – Período para calcular a inclinação de regressão linear.
- `TriggerShift` – Número de barras usadas para calcular a linha de gatilho.
- `EnableLong` – Permite entradas compradas.
- `EnableShort` – Permite entradas vendidas.
- `TakeProfitPercent` – Take-profit como percentual do preço de entrada.
- `StopLossPercent` – Stop-loss como percentual do preço de entrada.
- `CandleType` – Período das velas usado pela estratégia.

## Notas
- A estratégia opera apenas em velas completadas.
- A proteção via `StartProtection` aplica níveis fixos de take-profit e stop-loss baseados em percentual.
- Certifique-se de ter dados históricos suficientes para que o indicador possa formar seus valores.
