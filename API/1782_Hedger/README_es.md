# Estrategia de Cobertura (Hedger)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia coloca una orden limitada y una orden stop opuesta para cubrir la posición inicial. Funciona tanto en modo largo como corto e incorpora varios controles de riesgo.

La orden de cobertura protege la operación principal si el precio se mueve en la dirección incorrecta. Una regla de seguimiento 75-50 puede mover el stop a la mitad del objetivo una vez que se alcanza el 75% de la meta de beneficio. La cobertura de riesgo opcional puede abrir una orden de mercado contra la posición tras un movimiento adverso considerable, y el stop puede ajustarse tras un número configurable de ticks.

## Detalles

- **Criterios de entrada**: Colocar orden limitada en `EntryPrice` y stop de cobertura en `EntryPrice ± Spread`.
- **Largo/Corto**: Configurado mediante `IsLong`.
- **Criterios de salida**: Stop loss, take profit, regla 75-50 o cobertura de riesgo.
- **Stops**: Sí, con ajuste opcional.
- **Filtros**: Ninguno.

## Parámetros

- `EntryPrice` – precio para la orden pendiente.
- `StopLoss` – nivel de stop protector.
- `TakeProfit` – objetivo de beneficio.
- `Volume` – volumen de la orden.
- `Spread` – distancia para la orden de cobertura.
- `IsLong` – operación larga si es verdadero, corta si es falso.
- `UseRiskHedge` – abrir orden de mercado opuesta ante un fuerte movimiento adverso.
- `UseRiskSl` – ajustar el stop tras un movimiento adverso de `RiskSlTicks`.
- `RiskSlTicks` – número de ticks para el ajuste del stop de riesgo.
- `UseRule7550` – activar la regla de seguimiento 75-50.
