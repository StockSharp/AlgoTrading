# Estrategia de Reversión del Histograma RVI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia opera contra valores extremos del RVI. Opera con el Índice de Vigor Relativo (RVI) y abre posiciones cuando el indicador abandona zonas de sobrecompra o sobreventa, o cuando el RVI cruza su línea de señal. Se admiten dos modos de señal:

- **Levels** – reacciona cuando el RVI cruza umbrales superiores o inferiores predefinidos.
- **Cross** – reacciona cuando el RVI cruza su línea de señal.

La lógica es contraria a la tendencia: si el RVI estaba por encima del nivel alto y luego cae, se abre una posición larga. Si el RVI estaba por debajo del nivel bajo y luego sube, se abre una posición corta.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| `RviPeriod` | Período de cálculo del RVI. |
| `HighLevel` | Umbral superior del RVI. |
| `LowLevel` | Umbral inferior del RVI. |
| `Mode` | Modo de generación de señales (`Levels` o `Cross`). |
| `EnableBuyOpen` | Permitir abrir posiciones largas. |
| `EnableSellOpen` | Permitir abrir posiciones cortas. |
| `EnableBuyClose` | Permitir cerrar posiciones largas. |
| `EnableSellClose` | Permitir cerrar posiciones cortas. |
| `CandleType` | Marco temporal de las velas. |

## Cómo funciona

1. El RVI y su media móvil simple se calculan en cada vela completada.
2. Dependiendo del modo seleccionado, la estrategia verifica:
   - si el RVI abandona un nivel extremo, o
   - si el RVI cruza su línea de señal.
3. Con una señal larga, la estrategia cierra posiciones cortas y abre una posición larga. Con una señal corta, cierra posiciones largas y abre una posición corta.

El marco temporal por defecto es de cuatro horas.

## Notas

- Las órdenes se ejecutan con órdenes de mercado.
- La gestión de stop-loss y take-profit puede añadirse por separado si es necesario.
