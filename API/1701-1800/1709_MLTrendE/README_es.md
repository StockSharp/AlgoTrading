# Estrategia MLTrendE
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera en la dirección de una media móvil ponderada (WMA) y opcionalmente incrementa posiciones cuando el precio se mueve favorablemente.

## Lógica

- Calcular una WMA de la serie de velas seleccionada.
- Si no hay posición abierta:
  - **Tipo de operación 0**: abrir una posición larga cuando el precio de cierre está por encima de la WMA, o una posición corta cuando está por debajo.
  - **Tipo de operación 1**: siempre abrir una posición larga.
  - **Tipo de operación 2**: siempre abrir una posición corta.
- Cuando una posición está abierta y alcanza el objetivo de beneficio especificado, se añade otra operación con volumen escalado.
- Una vez alcanzado el número máximo de operaciones, toda la posición se cierra en el siguiente objetivo de beneficio.

## Parámetros

- `Volume` – volumen base de operación.
- `Multiplier1` – multiplicador de volumen para la segunda operación.
- `Multiplier2` – multiplicador de volumen para la tercera operación.
- `TakeProfit` – beneficio en unidades de precio requerido para escalar o cerrar.
- `Map` – período de la media móvil ponderada.
- `MaxTrades` – número máximo de operaciones consecutivas.
- `TradeType` – 0 seguimiento de tendencia, 1 forzar largo, 2 forzar corto.
- `CandleType` – marco temporal de las velas analizadas.

## Notas

La estrategia usa únicamente velas completadas y órdenes de mercado. No gestiona stops ni riesgo; use protección de cuenta si es necesario.
