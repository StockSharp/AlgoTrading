# Estrategia Omar MMR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Método basado en momentum que combina RSI, tres medias móviles exponenciales y un cruce de MACD. Las operaciones largas ocurren cuando el precio está por encima de la EMA lenta, la EMA rápida supera la EMA media, MACD cruza alcistamente y RSI se sitúa en una zona neutral entre 29 y 70.

Los porcentajes de take-profit y stop-loss se aplican a través del módulo de protección del motor. La configuración se centra en alinear momentum y tendencia mientras evita lecturas de RSI sobreextendidas.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Cierre por encima de EMA C, EMA A > EMA B, la línea MACD cruza por encima de la señal, RSI entre 29 y 70.
- **Criterios de salida**:
  - Gestionado a través de take-profit o stop-loss; sin salida explícita por indicador.
- **Indicadores**:
  - RSI (longitud 14)
  - EMA A/B/C (períodos 20/50/200)
  - MACD (12,26,9)
- **Stops**: Take-profit basado en porcentaje 1.5% y stop-loss 2% por defecto.
- **Valores predeterminados**:
  - `RsiLength` = 14
  - `EmaALength` = 20
  - `EmaBLength` = 50
  - `EmaCLength` = 200
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `TakeProfitPercent` = 1.5
  - `StopLossPercent` = 2.0
- **Filtros**:
  - Continuación de tendencia
  - Marco temporal único
  - Indicadores: RSI, EMA, MACD
  - Stops: Sí
  - Complejidad: Moderado
