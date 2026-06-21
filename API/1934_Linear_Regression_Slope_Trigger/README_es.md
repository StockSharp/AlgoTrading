# Estrategia de Disparador de Pendiente de Regresión Lineal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia utiliza un indicador de pendiente de regresión lineal y una línea disparadora derivada para identificar cambios de tendencia. Se abre una posición larga cuando la línea disparadora cruza hacia arriba la línea de pendiente, mientras que se abre una posición corta cuando la línea disparadora cruza hacia abajo la línea de pendiente. Las posiciones existentes se cierran cuando aparece una señal opuesta. El enfoque está inspirado en la estrategia MQL5 original "Exp_LinearRegSlopeV2".

## Lógica del indicador
1. La **Pendiente de Regresión Lineal** se calcula sobre los precios de cierre de velas durante un período configurable.
2. Una **línea disparadora** se calcula como `2 * slope - slope[Shift]`, donde `slope[Shift]` es el valor de pendiente de varios barones atrás.
3. Los cruces entre la línea disparadora y la línea de pendiente sirven como señales de trading.

## Reglas de trading
- **Entrar Largo:** El disparador cruza hacia arriba la pendiente y se permiten operaciones cortas.
- **Entrar Corto:** El disparador cruza hacia abajo la pendiente y se permiten operaciones largas.
- **Salir Largo:** La pendiente sube por encima del disparador.
- **Salir Corto:** El disparador sube por encima de la pendiente.

## Parámetros
- `SlopeLength` – Período para calcular la pendiente de regresión lineal.
- `TriggerShift` – Número de barras utilizadas para calcular la línea disparadora.
- `EnableLong` – Permite entradas largas.
- `EnableShort` – Permite entradas cortas.
- `TakeProfitPercent` – Take-profit como porcentaje del precio de entrada.
- `StopLossPercent` – Stop-loss como porcentaje del precio de entrada.
- `CandleType` – Marco temporal de velas utilizado por la estrategia.

## Notas
- La estrategia opera únicamente sobre velas completadas.
- La protección mediante `StartProtection` aplica niveles fijos de take-profit y stop-loss basados en porcentaje.
- Asegúrese de disponer de datos históricos suficientes para que el indicador pueda formar sus valores.
