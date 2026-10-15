# Estratégia de Reversão MACD Volume BBO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina o oscilador de volume com cruzamentos da linha zero do MACD e comparação de sinais.
Entra comprado quando o MACD cruza acima de zero com oscilador de volume positivo e MACD acima de sua linha de sinal.
As entradas vendidas são simétricas. O stop loss usa a mínima/máxima recente e o take profit é baseado na relação risco/recompensa.

## Parâmetros
- `VolumeShortLength` – período EMA curto para volume (padrão: 6)
- `VolumeLongLength` – período EMA longo para volume (padrão: 12)
- `MacdFastLength` – período de média rápida para MACD (padrão: 11)
- `MacdSlowLength` – período de média lenta para MACD (padrão: 21)
- `MacdSignalLength` – período da linha de sinal para MACD (padrão: 10)
- `LookbackPeriod` – barras para calcular a máxima/mínima recente (padrão: 10)
- `RiskReward` – razão take profit / stop loss (padrão: 1.5)
- `CandleType` – período das velas (padrão: 5 minutos)
