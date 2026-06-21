# Estrategia ForexProfitBoost
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia **ForexProfitBoost** es un sistema de trading de reversión que combina una Media Móvil Exponencial (EMA) rápida y una Media Móvil Simple (SMA) lenta. La estrategia espera que la EMA rápida cruce la SMA lenta y luego opera en contra de la dirección del cruce, esperando un retroceso del precio. Se pueden configurar niveles opcionales de stop-loss y take-profit en puntos de precio absolutos para la gestión del riesgo.

## Indicadores
- **EMA (rápida)**: periodo predeterminado de 7.
- **SMA (lenta)**: periodo predeterminado de 21.

## Reglas de trading
1. Suscribirse al marco temporal de velas seleccionado.
2. Calcular los valores de EMA y SMA en cada vela finalizada.
3. Cuando la EMA rápida cruza **por debajo** de la SMA lenta:
   - Cerrar cualquier posición corta.
   - Abrir una nueva posición larga.
4. Cuando la EMA rápida cruza **por encima** de la SMA lenta:
   - Cerrar cualquier posición larga.
   - Abrir una nueva posición corta.
5. Aplicar niveles de stop-loss y take-profit relativos al precio de entrada si se especifican.

## Parámetros
| Nombre | Descripción | Predeterminado |
|--------|-------------|----------------|
| `FastPeriod` | Periodo para la EMA rápida. | 7 |
| `SlowPeriod` | Periodo para la SMA lenta. | 21 |
| `StopLoss` | Distancia de stop-loss en puntos de precio. | 1000 |
| `TakeProfit` | Distancia de take-profit en puntos de precio. | 2000 |
| `CandleType` | Marco temporal utilizado para los cálculos. | 1 hora |

## Notas
- La estrategia utiliza la API de alto nivel de StockSharp y no almacena colecciones históricas.
- Las operaciones se ejecutan únicamente con órdenes de mercado después de que se completa una vela.
- Todos los comentarios en el código fuente están escritos en inglés según lo requerido.
