# Estrategia CSPA 1.43
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una adaptación del asesor experto MQL **CSPA-1_43**. Mide la fuerza de un par de divisas usando el Índice de Fuerza Relativa (RSI). Cuando el par se vuelve suficientemente fuerte o débil, la estrategia abre una posición en la dirección del momentum predominante y la cierra cuando el momentum se desvanece.

## Lógica

- Suscribirse a velas del valor seleccionado.
- Calcular el valor del RSI para cada vela completada.
- Abrir una posición larga cuando el RSI sube por encima del umbral superior.
- Abrir una posición corta cuando el RSI cae por debajo del umbral inferior.
- Cerrar la posición actual cuando el RSI regresa a la zona neutral.

## Parámetros

| Nombre | Descripción | Predeterminado |
|--------|-------------|----------------|
| `StrengthPeriod` | Período utilizado para el indicador RSI. | `14` |
| `Threshold` | Distancia desde el nivel neutral del RSI de 50 utilizada para generar señales. | `10` |
| `CandleType` | Marco temporal de las velas. | `1 hora` |

## Notas

- La estrategia utiliza la API de alto nivel con vinculación automática de indicadores.
- Las órdenes se ejecutan usando órdenes de mercado (`BuyMarket` y `SellMarket`).
- Solo se procesan las velas completadas.
