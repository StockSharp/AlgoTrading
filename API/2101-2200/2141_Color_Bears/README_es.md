# Estrategia Color Bears
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia construye un oscilador Bears Power doblemente suavizado y opera con los cambios en su pendiente.

## Idea
1. Calcular una media móvil exponencial (MA1) de los precios de cierre.
2. Calcular Bears Power como la diferencia entre el mínimo de la vela y MA1.
3. Suavizar Bears Power con otra media móvil exponencial (MA2).
4. Rastrear si el valor suavizado sube o baja y reaccionar a las reversiones de pendiente.

## Reglas de trading
- Cuando el indicador pasa de subir a bajar (color 0 → 2), cerrar posiciones cortas y abrir una larga.
- Cuando el indicador pasa de bajar a subir (color 2 → 0), cerrar posiciones largas y abrir una corta.
- Cada posición usa la propiedad `Volume` de la estrategia como tamaño de orden.

## Parámetros
| Nombre | Descripción |
|--------|-------------|
| `Ma1Period` | Período del primer EMA utilizado para construir Bears Power. |
| `Ma2Period` | Período del EMA de suavizado. |
| `CandleType` | Marco temporal de velas para los cálculos. |

## Notas
Esta implementación en C# está adaptada del experto MQL "ColorBears" (carpeta `MQL/14314`).
El algoritmo se basa en los indicadores estándar de StockSharp y los bindings de API de alto nivel.
