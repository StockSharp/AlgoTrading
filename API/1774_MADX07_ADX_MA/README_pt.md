# Estratégia MADX-07 ADX MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia foi convertida do consultor especialista MQL4 MADX-07. Opera em velas H4 e combina duas médias móveis com o Índice de Movimento Direcional Médio (ADX) como filtros.

## Lógica

- Entrada comprada: Preço acima da MA lenta, MA rápida acima da MA lenta, preço pelo menos `MaDifference` pontos acima da MA rápida nas duas últimas velas, ADX subindo acima de `AdxMainLevel` com +DI subindo e -DI caindo.
- Entrada vendida: Condições espelho.
- A posição é fechada quando o lucro em pontos atinge `CloseProfit` ou quando uma ordem limitada a uma distância de `TakeProfit` é executada.

## Parâmetros

- `BigMaPeriod` (25) – período da MA mais lenta.
- `BigMaType` – tipo da MA mais lenta.
- `SmallMaPeriod` (5) – período da MA mais rápida.
- `SmallMaType` – tipo da MA mais rápida.
- `MaDifference` (5) – distância mínima entre o preço e a MA rápida em pontos.
- `AdxPeriod` (11) – período de cálculo do ADX.
- `AdxMainLevel` (13) – valor mínimo do ADX.
- `AdxPlusLevel` (13) – valor mínimo do +DI.
- `AdxMinusLevel` (14) – valor mínimo do -DI.
- `TakeProfit` (299) – distância do take-profit em pontos.
- `CloseProfit` (13) – lucro em pontos para saída antecipada.
- `Volume` (0.1) – volume de negociação.
- `CandleType` – período das velas (padrão H4).
