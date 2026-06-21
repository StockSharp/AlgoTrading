# Estratégia GO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia calcula um valor composto **GO** com base em médias móveis exponenciais (EMA) dos preços de abertura, máxima, mínima e fechamento multiplicados pelo volume. As decisões de trading são tomadas de acordo com o sinal e o nível do valor GO.

## Fórmula

`GO = ((C - O) + (H - O) + (L - O) + (C - L) + (C - H)) * V`

Onde:
- `C`, `O`, `H`, `L` – valores EMA dos preços de Fechamento, Abertura, Máxima e Mínima.
- `V` – volume da vela processada.

## Regras de trading

- **Abrir Comprado**: GO > `OpenLevel`
- **Abrir Vendido**: GO < `-OpenLevel`
- **Fechar Comprado**: GO < (`OpenLevel` - `CloseLevelDiff`)
- **Fechar Vendido**: GO > -(`OpenLevel` - `CloseLevelDiff`)

## Parâmetros

| Nome | Descrição |
|------|-----------|
| `MaPeriod` | Período EMA para suavização de preços. |
| `OpenLevel` | Nível GO para acionar novas posições. |
| `CloseLevelDiff` | Diferença entre os níveis de abertura e fechamento. |
| `ShowGo` | Se os valores GO são registrados. |
| `CandleType` | Tipo de velas usadas para o processamento. |

A estratégia opera em velas concluídas e usa ordens a mercado para gerenciamento de posições.
