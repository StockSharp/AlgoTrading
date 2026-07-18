# Estratégia SpectrAnalysis Chaikin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa o oscilador Chaikin para detectar mudanças de momentum. O oscilador é calculado a partir da linha de Acumulação/Distribuição suavizada por duas médias móveis ponderadas lineares. Quando a inclinação do oscilador vira para cima e o último valor cruza acima do valor anterior, uma posição comprada é aberta. Ao contrário, quando a inclinação vira para baixo e o último valor cruza abaixo do valor anterior, uma posição vendida é aberta.

## Parâmetros

| Nome | Descrição |
|------|-----------|
| `FastMaPeriod` | Período da média móvel ponderada linear rápida usada no oscilador Chaikin. |
| `SlowMaPeriod` | Período da média móvel ponderada linear lenta usada no oscilador Chaikin. |
| `BuyPosOpen` | Habilitar abertura de posições compradas. |
| `SellPosOpen` | Habilitar abertura de posições vendidas. |
| `BuyPosClose` | Habilitar fechamento de posições compradas quando as condições forem atendidas. |
| `SellPosClose` | Habilitar fechamento de posições vendidas quando as condições forem atendidas. |
| `CandleType` | Período das velas usadas para o cálculo. |

## Notas

- Ordens a mercado são usadas para entradas e saídas.
- A estratégia não define ordens de stop-loss ou take-profit.
- Apenas a versão em C# é fornecida; não há implementação em Python.
