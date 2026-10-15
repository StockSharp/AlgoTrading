# Estrategia SAW System 1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia de ruptura coloca órdenes stop al inicio de cada día de trading. Mide el rango diario promedio durante un número configurable de días y utiliza ese valor para derivar los niveles de stop-loss y take-profit. Las órdenes se posicionan en ambos lados del precio actual y solo se espera que uno de ellos se active.

En el `OpenHour` especificado, la estrategia calcula los precios de buy stop y sell stop a la mitad de la distancia de stop-loss desde el precio de mercado actual. Los niveles de stop-loss y take-profit se definen como porcentajes del rango promedio. Cuando se ejecuta una orden stop, la orden opuesta puede cancelarse o mantenerse para revertir la posición. Una función opcional de martingala multiplica el volumen de la orden restante tras una ejecución.

Las órdenes de entrada pendientes que no se ejecuten antes de `CloseHour` se eliminan para evitar exposición durante la noche. Tras una entrada, la estrategia coloca inmediatamente órdenes protectoras de stop-loss y take-profit relativas al precio de ejecución.

## Detalles

- **Criterios de entrada:**
  - Calcular el rango diario promedio usando un ATR durante `VolatilityDays` días.
  - Calcular las distancias de stop-loss y take-profit como `StopLossRate` y `TakeProfitRate` por ciento de ese rango.
  - En `OpenHour` colocar órdenes buy y sell stop a `offset = stopLoss/2` del precio de mercado.
- **Criterios de salida:**
  - Las órdenes protectoras de stop-loss y take-profit cierran posiciones.
  - Las órdenes de entrada pendientes se cancelan en `CloseHour`.
- **Modo inverso:**
  - Si `Reverse` es verdadero, la orden stop opuesta permanece para revertir la posición.
  - Si `UseMartingale` también es verdadero, la orden restante se re-registra con el volumen multiplicado por `MartingaleMultiplier`.
- **Largo/Corto:** Ambas direcciones.
- **Stops:** Stop-loss y take-profit fijos basados en el rango diario.
- **Valores predeterminados:**
  - `VolatilityDays` = 5
  - `OpenHour` = 7
  - `CloseHour` = 10
  - `StopLossRate` = 15%
  - `TakeProfitRate` = 30%
  - `Reverse` = false
  - `UseMartingale` = false
  - `MartingaleMultiplier` = 2.0

Este enfoque intenta capturar rupturas tras sesiones nocturnas tranquilas, limitando el riesgo mediante objetivos ajustados a la volatilidad.
