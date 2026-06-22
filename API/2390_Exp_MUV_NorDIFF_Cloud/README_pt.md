# Exp MUV NorDIFF Cloud
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no momentum normalizado de SMA e EMA.
Entra comprado quando o momentum de SMA ou EMA atinge +100 e vendido quando atinge -100.

## Parâmetros
- `MaPeriod` – período da média móvel.
- `MomentumPeriod` – número de barras usadas para o cálculo do momentum.
- `KPeriod` – janela para normalização dos extremos do momentum.
- `CandleType` – período dos candles.

## Notas
A estratégia calcula os valores de SMA e EMA, mede seu momentum e o normaliza dentro do intervalo recente para gerar sinais de negociação.
