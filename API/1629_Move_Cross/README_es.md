# Estrategia Move Cross
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia demuestra una conversión simplificada del script original `move_cross.mq4`. Emplea el indicador RAVI (Range Action Verification Index) calculado a partir de dos medias móviles simples para determinar la dirección de la tendencia.

La estrategia compara los valores RAVI horarios y diarios:

- **Comprar** cuando el RAVI horario es negativo mientras el RAVI diario es positivo y creciente.
- **Vender** cuando el RAVI horario es positivo mientras el RAVI diario es negativo y decreciente.

Las posiciones se abren a mercado con objetivo de ganancia y stop-loss opcionales.

## Parámetros

| Nombre     | Descripción                          | Predeterminado |
|------------|--------------------------------------|----------------|
| TakeProfit | Objetivo de ganancia en puntos        | 50             |
| StopLoss   | Límite de pérdida en puntos           | 100            |

## Notas

- La estrategia usa dos pares de SMA (períodos 2 y 24) para calcular el RAVI en velas horarias y diarias.
- Está pensada para fines educativos y puede requerir ajuste adicional para trading en vivo.
