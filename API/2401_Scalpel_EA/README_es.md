# Estrategia Scalpel EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una conversión simplificada del *Scalpel EA* original escrito para MetaTrader.
Combina un filtro de Índice de Canal de Materias Primas (CCI) con análisis de ruptura en múltiples marcos temporales. El objetivo es operar en la dirección del momentum a corto plazo cuando varios marcos temporales superiores confirman el movimiento.

## Lógica

1. **Indicador** – CCI calculado en el marco temporal principal. Las operaciones solo se permiten cuando el valor CCI permanece dentro de una banda configurable alrededor de cero.
2. **Confirmación de tendencia** – Para velas de 30 minutos, 1 hora y 4 horas, los máximos y mínimos más recientes se comparan con los anteriores.
   - Las operaciones largas requieren mínimos ascendentes en los tres marcos temporales.
   - Las operaciones cortas requieren máximos descendentes en los tres marcos temporales.
3. **Ruptura** – La entrada se activa cuando el precio de cierre de la vela principal rompe el máximo (para largos) o el mínimo (para cortos) de la vela anterior.
4. **Control de riesgo** – `StartProtection` coloca un take-profit y stop-loss fijos medidos en unidades de precio.

## Parámetros

| Nombre | Descripción |
| ---- | ----------- |
| `CciPeriod` | Período del Índice de Canal de Materias Primas. |
| `CciLimit` | Umbral absoluto de CCI. Las entradas solo se permiten dentro de ±límite. |
| `TakeProfit` | Valor de take profit en unidades de precio. |
| `StopLoss` | Valor de stop loss en unidades de precio. |
| `CandleType` | Marco temporal principal para el trading (predeterminado 1 minuto). |

## Notas

- La estrategia se suscribe a velas adicionales de 30 minutos, 1 hora y 4 horas para evaluar las tendencias de marcos temporales superiores.
- El volumen se toma de la propiedad `Strategy.Volume` de la clase base.
- Solo hay una posición abierta a la vez. Las señales opuestas cierran la posición existente y abren una nueva.
