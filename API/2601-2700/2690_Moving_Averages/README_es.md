# Estrategia de Medias Móviles
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General
La estrategia de Medias Móviles replica el experto clásico de MetaTrader que opera cruzamientos del precio respecto a una media móvil simple (SMA) desplazada. El algoritmo procesa únicamente velas completadas, asegurando que todas las decisiones de trading se basen en barras completamente formadas. El dimensionamiento de posición sigue un modelo de riesgo dinámico vinculado al capital de la cuenta y se adapta a rachas de pérdidas, imitando la implementación MQL original.

## Lógica de Trading
- Se calcula una media móvil simple con un período configurable y un desplazamiento hacia adelante adicional medido en barras completadas.
- En cada vela completada, la estrategia verifica si la barra abrió por encima de la SMA desplazada y cerró por debajo (cruce bajista) o abrió por debajo y cerró por encima (cruce alcista).
- El sistema solo gestiona una posición a la vez. Cuando ocurre un cruce contra la posición activa, la posición se cierra primero y no se envían órdenes de reversión en la misma barra.
- Si no hay posición abierta:
  - Un cruce alcista abre una posición larga.
  - Un cruce bajista abre una posición corta.

## Gestión de Posiciones
- Las posiciones largas se cierran cuando ocurre un cruce bajista.
- Las posiciones cortas se cierran cuando ocurre un cruce alcista.
- La ejecución de operaciones usa órdenes de mercado en el instrumento de la estrategia.
- Se registra el historial de operaciones para calcular el precio de entrada efectivo y poder medir ganancias y pérdidas al cerrar la posición.

## Gestión de Riesgos y Dimensionamiento de Posición
- El volumen base de orden se deriva del capital del portafolio multiplicado por el parámetro **Maximum Risk**, dividido por el precio de cierre actual. Si el capital no está disponible, la estrategia usa el volumen predeterminado de la estrategia.
- Un parámetro **Decrease Factor** reduce el volumen de orden calculado cuando se detectan operaciones perdedoras consecutivas. La reducción es proporcional a la racha de pérdidas, reproduciendo la lógica de dimensionamiento adaptativo de la versión MQL.
- El volumen de orden nunca es negativo; cuando la reducción compensa completamente el monto base, la operación se omite.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `MaximumRisk` | Fracción del capital de la cuenta arriesgada en cada operación. | `0.02` |
| `DecreaseFactor` | Divisor usado para reducir el volumen después de pérdidas consecutivas. | `3` |
| `MovingPeriod` | Período de la SMA utilizada para señales. | `12` |
| `MovingShift` | Número de barras completadas usadas para desplazar la SMA hacia adelante. | `6` |
| `CandleType` | Serie de velas usada para cálculos (marco temporal). | Velas `5m` |

## Notas
- El desplazamiento de la media móvil se implementa mediante un búfer circular interno para que la estrategia use el valor de la SMA de varias barras atrás, igual que el parámetro de desplazamiento del indicador de MetaTrader.
- Las órdenes solo se generan cuando tanto la SMA como el búfer desplazado están completamente formados, evitando operaciones prematuras durante el calentamiento.
- Los mensajes de registro documentan entradas, salidas y resultados de operaciones para facilitar la depuración y el análisis de rendimiento.
