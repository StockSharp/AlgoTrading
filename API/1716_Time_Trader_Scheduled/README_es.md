# Estrategia de Operador por Hora Programada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia envía órdenes de mercado a una hora predefinida y las protege con niveles fijos de stop loss y toma de ganancias.

## Reglas de Trading

- Cuando la hora actual alcanza `Trade Hour:Trade Minute:Trade Second`, la estrategia se activa una vez por sesión.
- Si `Allow Buy` está habilitado, se abre una posición larga con el `Volume` especificado.
- Si `Allow Sell` está habilitado, se abre una posición corta con el mismo `Volume`.
- Las órdenes protectoras se gestionan mediante `StartProtection` usando valores en puntos para el stop loss y la toma de ganancias.

## Parámetros

| Nombre | Descripción |
| ------ | ----------- |
| `Volume` | Tamaño de la orden. |
| `Take Profit (ticks)` | Distancia de la toma de ganancias desde la entrada en ticks. |
| `Stop Loss (ticks)` | Distancia del stop loss desde la entrada en ticks. |
| `Allow Buy` | Habilitar operaciones largas. |
| `Allow Sell` | Habilitar operaciones cortas. |
| `Trade Hour` | Hora del día para operar (0-23). |
| `Trade Minute` | Minuto de la hora para operar (0-59). |
| `Trade Second` | Segundo del minuto para operar (0-59). |
| `Candle Type` | Serie de velas usadas para rastrear el tiempo, por defecto velas de 1 segundo. |

## Notas

La estrategia abre operaciones solo una vez por ejecución. Para operar de nuevo, reinicie la estrategia o ajuste la hora de operación.
