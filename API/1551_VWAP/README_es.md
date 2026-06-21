# Estrategia VWAP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Utiliza VWAP con bandas de entrada y múltiples modos de salida. Compra cuando el precio cierra por encima de la banda inferior y vende cuando cierra por debajo de la banda superior. Admite salidas por VWAP o por banda de desviación y una salida de seguridad opcional tras velas consecutivas opuestas.

## Parámetros

- **StopPoints**: Búfer de stop desde la vela de señal.
- **ExitModeLong**: Modo de salida para posiciones largas.
- **ExitModeShort**: Modo de salida para posiciones cortas.
- **TargetLongDeviation**: Multiplicador de desviación para objetivo largo.
- **TargetShortDeviation**: Multiplicador de desviación para objetivo corto.
- **EnableSafetyExit**: Activar salida de seguridad tras velas opuestas.
- **NumOpposingBars**: Número de velas opuestas para la salida de seguridad.
- **AllowLongs**: Permitir operaciones largas.
- **AllowShorts**: Permitir operaciones cortas.
- **MinStrength**: Fuerza mínima de señal.
- **CandleType**: Tipo de velas.
