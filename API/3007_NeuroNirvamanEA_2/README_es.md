# Neuro Nirvaman EA 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Neuro Nirvaman EA 2 es una estrategia de perceptrón multicapa que fue escrita originalmente para MetaTrader 5. La lógica combina cuatro flujos +DI suavizados por Laguerre con dos detectores de ruptura SilverTrend. Cada barra la estrategia evalúa tres perceptrones cuyos pesos están controlados por los parámetros X. Un módulo supervisor elige qué salida del perceptrón debe negociarse según el modo de paso seleccionado. Las operaciones se permiten solo dentro de la ventana de sesión configurada y todas las posiciones se aplanan una vez que la ventana se cierra.

## Indicadores y señales
- **Filtros Laguerre +DI** – Cada bloque Laguerre suaviza el valor +DI de un indicador ADX (gamma = 0.764). El valor resultante oscila entre 0 y 1 y se compara con una línea central de 0.5 con umbrales de distancia definidos por el usuario.
- **Ruptura SilverTrend** – Dos detectores SilverTrend calculan envolventes dinámicas de soporte/resistencia usando las últimas nueve barras. El ajuste de riesgo modifica el ancho de la envolvente (`K = 33 - risk`). Una transición de bajista a alcista (o viceversa) produce señales ±1 que alimentan los perceptrones.

## Lógica de negociación
1. **Perceptrón #1** usa Laguerre #1 para el componente de tensión y SilverTrend #1 para el componente de ruptura. Los pesos `X11` y `X12` desplazan las contribuciones relativas a 100.
2. **Perceptrón #2** refleja el primer perceptrón pero se basa en Laguerre #2 y SilverTrend #2 con pesos `X21` y `X22`.
3. **Perceptrón #3** combina las salidas de tensión de Laguerre #3 y Laguerre #4 ponderadas por `X31` y `X32`.
4. **Modos supervisor (`Pass`)**
   - `1` – Negociar el perceptrón #1 (`< 0` abre corto, de lo contrario largo).
   - `2` – Negociar el perceptrón #2 (`> 0` abre largo, de lo contrario corto).
   - `3` – Abrir una posición larga cuando tanto el perceptrón #3 como el #2 son positivos. Abrir un corto cuando el perceptrón #3 es no positivo y el perceptrón #1 es negativo.
   - `4` – Deshabilitar negociación (coincide con el comportamiento predeterminado del EA original).

Cada entrada coloca una orden de mercado de volumen fijo y registra niveles de stop-loss / take-profit expresados en pasos de precio. Las posiciones se monitorean en cada vela finalizada: si el máximo/mínimo perfora los objetivos registrados, la estrategia sale inmediatamente. Salir de la ventana de negociación también fuerza una salida.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `Risk1`, `Risk2` | Configuraciones de riesgo SilverTrend. Los valores más altos reducen la envolvente y generan señales más frecuentes. |
| `LaguerreXPeriod` | Longitud ADX que alimenta el suavizador Laguerre (para cada uno de los cuatro flujos). |
| `LaguerreXDistance` | Distancia porcentual alrededor de la línea central 0.5 que define la tensión alcista/bajista. |
| `X11`, `X12`, `X21`, `X22`, `X31`, `X32` | Pesos del perceptrón (los valores se desplazan en 100 dentro de la fórmula, exactamente como en la versión MQL). |
| `TakeProfit1`, `StopLoss1`, `TakeProfit2`, `StopLoss2` | Distancias de objetivo de beneficio y stop de protección en pasos de precio para las respectivas señales del perceptrón. |
| `Pass` | Selector de modo supervisor (1–4). |
| `TradeVolume` | Tamaño base de orden usado para entradas de mercado. |
| `StartHour`, `StartMinute`, `EndHour`, `EndMinute` | Límites de la sesión de negociación. Cuando la hora actual está fuera de esta ventana, todas las posiciones se cierran y no se permiten nuevas operaciones. |
| `CandleType` | Suscripción de velas para impulsar la estrategia de alto nivel. |

## Gestión de riesgos
La estrategia depende de las distancias fijas de stop-loss y take-profit definidas por el perceptrón que activó la entrada. No se realiza piramidación ni promediado. Porque la lógica solo opera cuando no hay posición abierta, la exposición está limitada a una sola posición activa y todas las operaciones se cierran a la fuerza una vez que termina la ventana de sesión.

## Notas
- El gamma para el suavizador Laguerre está fijo en 0.764 para coincidir con la implementación MQL.
- El valor Pass `4` mantiene la estrategia inactiva, lo que refleja el predeterminado de seguridad del EA original.
- Los cálculos SilverTrend usan primitivos de indicador (highest, lowest, simple moving average) en lugar de buffers personalizados para cumplir con las directrices de StockSharp.
