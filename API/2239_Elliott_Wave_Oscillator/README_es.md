# Estrategia del Oscilador de Olas de Elliott
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia aplica el Oscilador de Olas de Elliott (EWO) sobre los cierres de velas. El EWO se calcula como la diferencia entre una Media Móvil Simple rápida y una lenta (5 y 35 períodos por defecto). La lógica de trading busca puntos de giro en el oscilador para capturar posibles reversiones de tendencia.

Se abre una posición larga cuando el oscilador forma un mínimo local y comienza a subir. Se abre una posición corta cuando el oscilador forma un máximo local y comienza a caer. Las posiciones existentes se invierten en consecuencia. Se admiten take‑profit y stop‑loss basados en porcentaje opcionales a través de `StartProtection`.

## Detalles

- **Indicador**: Oscilador de Olas de Elliott = SMA(rápida) − SMA(lenta).
- **Criterios de entrada**:
  - **Largo**: el valor del oscilador estaba bajando y luego gira hacia arriba.
  - **Corto**: el valor del oscilador estaba subiendo y luego gira hacia abajo.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: La posición se invierte ante la señal opuesta o sale por stop o take‑profit.
- **Stops**: Stop‑loss y take‑profit en porcentaje.
- **Filtros**: Ninguno.
