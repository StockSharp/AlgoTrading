# Estratégia VWAP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Utiliza VWAP com bandas de entrada e múltiplos modos de saída. Compra quando o preço fecha acima da banda inferior e vende quando fecha abaixo da banda superior. Suporta saídas por VWAP ou banda de desvio e saída de segurança opcional após velas consecutivas contrárias.

## Parâmetros

- **StopPoints**: Buffer de stop a partir da vela de sinal.
- **ExitModeLong**: Modo de saída para posições compradas.
- **ExitModeShort**: Modo de saída para posições vendidas.
- **TargetLongDeviation**: Multiplicador de desvio para alvo comprado.
- **TargetShortDeviation**: Multiplicador de desvio para alvo vendido.
- **EnableSafetyExit**: Ativar saída de segurança após velas contrárias.
- **NumOpposingBars**: Número de velas contrárias para a saída de segurança.
- **AllowLongs**: Permitir operações compradas.
- **AllowShorts**: Permitir operações vendidas.
- **MinStrength**: Força mínima do sinal.
- **CandleType**: Tipo de velas.
