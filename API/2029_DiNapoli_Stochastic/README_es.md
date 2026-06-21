# Estrategia DiNapoli Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa un sistema de trading basado en el oscilador **DiNapoli Stochastic**. Reacciona a los cruces entre las líneas %K y %D del indicador estocástico.

## Lógica de la estrategia

1. Suscribirse a las velas del marco temporal seleccionado.
2. Calcular los valores del DiNapoli Stochastic usando el oscilador Stochastic estándar con períodos de suavizado.
3. Cerrar posiciones cortas cuando el %K previo estaba por encima del %D.
4. Cerrar posiciones largas cuando el %K previo estaba por debajo del %D.
5. Abrir una posición larga cuando %K cruza por debajo de %D y se permiten operaciones largas.
6. Abrir una posición corta cuando %K cruza por encima de %D y se permiten operaciones cortas.

## Parámetros

- `FastK` – período base para %K.
- `SlowK` – período de suavizado para %K.
- `SlowD` – período de suavizado para %D.
- `BuyOpen` – habilitar o deshabilitar entradas largas.
- `SellOpen` – habilitar o deshabilitar entradas cortas.
- `BuyClose` – habilitar o deshabilitar el cierre de posiciones largas.
- `SellClose` – habilitar o deshabilitar el cierre de posiciones cortas.
- `CandleType` – marco temporal de velas utilizado para los cálculos.

## Notas

La estrategia utiliza la API de alto nivel de StockSharp y procesa únicamente velas finalizadas. Los valores del indicador se obtienen a través de `BindEx` sin utilizar solicitudes de valores históricos.
