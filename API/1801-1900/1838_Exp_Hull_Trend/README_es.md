# Estrategia de Tendencia Exp Hull
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La Estrategia de Tendencia Exp Hull se basa en el indicador Hull Moving Average (HMA). El algoritmo compara un cálculo Hull intermedio con una media móvil Hull suavizada. Cuando la línea Hull rápida cruza por encima de la línea suavizada más lenta, la estrategia abre una posición larga. Cuando la línea rápida cruza por debajo de la línea suavizada, la estrategia abre una posición corta.

## Lógica de la estrategia

1. Calcular una media móvil ponderada (WMA) del precio de cierre con período **Length / 2**.
2. Calcular otra WMA del precio de cierre con período **Length**.
3. Construir el valor Hull intermedio: `fast = 2 * WMA(Length/2) - WMA(Length)`.
4. Suavizar el valor intermedio con una WMA de período `sqrt(Length)` para obtener el valor Hull final `slow`.
5. Generar señales:
   - **Entrada Largo** – cuando `fast` cruza por encima de `slow`.
   - **Entrada Corto** – cuando `fast` cruza por debajo de `slow`.
6. Las posiciones se revierten en señales opuestas. Las órdenes de protección se gestionan a través de `StartProtection`.

## Parámetros

| Nombre | Descripción |
|--------|-------------|
| `Hull Length` | Período base para el cálculo Hull. Determina la sensibilidad de ambas WMA. |
| `Candle Type` | Marco temporal de velas utilizado para los cálculos del indicador. |

## Notas

- La estrategia trabaja únicamente con velas completadas.
- Los valores del indicador se vinculan a través de la API de alto nivel para evitar colecciones de datos manuales.
- El volumen se toma de la configuración de la estrategia; cuando cambia la dirección de la señal, la posición se revierte.
