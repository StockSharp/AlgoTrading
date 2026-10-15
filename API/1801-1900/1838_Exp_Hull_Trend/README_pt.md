# Estratégia de Tendência Exp Hull
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A Estratégia de Tendência Exp Hull é baseada no indicador Hull Moving Average (HMA). O algoritmo compara um cálculo Hull intermediário com uma média móvel Hull suavizada. Quando a linha Hull mais rápida cruza acima da linha suavizada mais lenta, a estratégia abre uma posição comprada. Quando a linha rápida cruza abaixo da linha suavizada, a estratégia abre uma posição vendida.

## Lógica da estratégia

1. Calcular uma média móvel ponderada (WMA) do preço de fechamento com período **Length / 2**.
2. Calcular outra WMA do preço de fechamento com período **Length**.
3. Construir o valor Hull intermediário: `fast = 2 * WMA(Length/2) - WMA(Length)`.
4. Suavizar o valor intermediário com uma WMA de período `sqrt(Length)` para obter o valor Hull final `slow`.
5. Gerar sinais:
   - **Entrada Comprado** – quando `fast` cruza acima de `slow`.
   - **Entrada Vendido** – quando `fast` cruza abaixo de `slow`.
6. As posições são revertidas em sinais opostos. Ordens de proteção são gerenciadas através de `StartProtection`.

## Parâmetros

| Nome | Descrição |
|------|-----------|
| `Hull Length` | Período base para o cálculo Hull. Determina a sensibilidade de ambas as WMAs. |
| `Candle Type` | Período de velas usado para cálculos do indicador. |

## Notas

- A estratégia trabalha apenas com velas completadas.
- Os valores do indicador são vinculados via API de alto nível para evitar coleções de dados manuais.
- O volume é retirado das configurações da estratégia; quando a direção do sinal muda, a posição é revertida.
