# Estrategia ColorXdinMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia ColorXdinMA implementa el indicador XdinMA, calculado como `ma_main * 2 - ma_plus`, donde ambos componentes son medias móviles simples con diferentes longitudes. La estrategia monitorea la pendiente de esta línea y abre posiciones cuando la pendiente cambia de dirección.

## Lógica de trading
- Cuando el indicador estaba decayendo y gira hacia arriba, se abre una posición larga. Las posiciones cortas existentes se cierran.
- Cuando el indicador estaba subiendo y gira hacia abajo, se abre una posición corta. Las posiciones largas existentes se cierran.

Solo se procesan velas completadas. Las órdenes se ejecutan a mercado.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `MainLength` | Período de la media móvil principal. | 10 |
| `PlusLength` | Período de la media móvil adicional. | 20 |
| `CandleType` | Marco temporal de las velas utilizadas para el cálculo. | 6 horas |

## Notas
Esta implementación es una estrategia StockSharp de alto nivel y puede ampliarse con funciones de gestión de riesgos o visualización según sea necesario.
