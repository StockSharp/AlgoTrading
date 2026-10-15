# Estrategia ADX Smoothed Cross
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen

La estrategia opera basándose en un índice de dirección promedio (ADX) de doble suavizado. Compara las líneas +DI y -DI suavizadas para detectar cambios de tendencia. Cuando la línea +DI suavizada cruza por encima de la línea -DI suavizada, la estrategia entra en una posición larga. Cuando la línea +DI suavizada cruza por debajo de la línea -DI suavizada, abre una posición corta.

## Cómo Funciona

- Utiliza un indicador ADX con período configurable.
- Aplica dos pasadas de suavizado exponencial controladas por los parámetros **Alpha1** y **Alpha2**.
- Una señal larga ocurre cuando el +DI suavizado anterior estaba por debajo del -DI suavizado y el +DI suavizado actual está por encima.
- Una señal corta ocurre en el cruce opuesto.
- Parámetros opcionales permiten deshabilitar operaciones largas o cortas y controlar si las posiciones existentes pueden cerrarse cuando aparece una señal opuesta.
- La gestión de riesgo incorporada establece niveles de stop-loss y take-profit en puntos.

## Parámetros

| Nombre | Descripción |
| ---- | ----------- |
| `AdxPeriod` | Período para el cálculo del ADX. |
| `Alpha1` | Primer coeficiente de suavizado (0-1). |
| `Alpha2` | Segundo coeficiente de suavizado (0-1). |
| `StopLoss` | Stop-loss en puntos. |
| `TakeProfit` | Take-profit en puntos. |
| `AllowBuy` | Habilitar entradas largas. |
| `AllowSell` | Habilitar entradas cortas. |
| `AllowCloseBuy` | Permitir cerrar posiciones largas en señales de venta. |
| `AllowCloseSell` | Permitir cerrar posiciones cortas en señales de compra. |
| `CandleType` | Marco temporal utilizado para el indicador. |

## Notas

- Solo se procesan velas finalizadas.
- La estrategia usa la API de alto nivel con vinculación de indicadores.
- El stop-loss y el take-profit se gestionan mediante `StartProtection`.
