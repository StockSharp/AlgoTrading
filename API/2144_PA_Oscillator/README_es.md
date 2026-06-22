# Estrategia PA Oscilador
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un port del experto MQL5 **Exp_PA_Oscillator.mq5**. Aplica dos medias móviles exponenciales (EMAs) a los precios de cierre de las velas y analiza la derivada de su diferencia.

## Lógica

1. Calcular EMAs rápida y lenta.
2. Calcular la diferencia entre ellas y rastrear su cambio respecto al valor anterior.
3. Determinar un código de color para la derivada:
   - **0** – la derivada es positiva y el MACD está subiendo.
   - **1** – la derivada es cero.
   - **2** – la derivada es negativa y el MACD está bajando.
4. Usar los colores de las dos últimas velas completadas para generar señales:
   - Hace dos barras el color era `0` y la barra anterior cambió desde `0` → abrir posición larga y cerrar posición corta.
   - Hace dos barras el color era `2` y la barra anterior cambió desde `2` → abrir posición corta y cerrar posición larga.

## Parámetros

| Nombre | Descripción |
| ------ | ----------- |
| `FastLength` | Longitud del EMA rápido. |
| `SlowLength` | Longitud del EMA lento. |
| `BuyPosOpen` | Habilitar la apertura de posiciones largas. |
| `SellPosOpen` | Habilitar la apertura de posiciones cortas. |
| `BuyPosClose` | Habilitar el cierre de posiciones largas. |
| `SellPosClose` | Habilitar el cierre de posiciones cortas. |
| `CandleType` | Marco temporal de velas utilizado para los cálculos. |

## Notas

- Solo se procesan velas completadas.
- Se usan órdenes de mercado para entradas y salidas.
- Esta implementación se centra en la claridad y los fines educativos, no en la rentabilidad.
