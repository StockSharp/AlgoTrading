# Estrategia de Pendiente Personalizada TEMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de reversión que utiliza cambios de pendiente de una Triple Exponential Moving Average (TEMA). El indicador se calcula en el marco temporal especificado y la estrategia reacciona a los cambios de dirección.

## Cómo funciona

- **Criterios de entrada**:
  - **Largo**: TEMA estaba cayendo y se gira hacia arriba.
  - **Corto**: TEMA estaba subiendo y se gira hacia abajo.
- **Criterios de salida**: la señal inversa cierra la posición existente.
- **Indicadores**: Triple Exponential Moving Average.

## Parámetros clave

- `TemaLength` – Número de barras para el cálculo de TEMA.
- `CandleType` – Marco temporal de las velas utilizadas para el análisis.
