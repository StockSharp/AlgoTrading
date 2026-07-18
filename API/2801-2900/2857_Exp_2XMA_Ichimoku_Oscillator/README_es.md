# Estrategia de Oscilador Exp 2XMA Ichimoku
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia reproduce la lógica del asesor experto original de MetaTrader "Exp_2XMA_Ichimoku_Oscillator" combinando dos envolventes de precio estilo Ichimoku suavizadas con medias móviles configurables. La implementación en StockSharp utiliza la API de estrategia de alto nivel y se centra en la generación de señales basada en velas, manteniendo las reglas de gestión de posición del algoritmo fuente.

## Idea principal

1. Se calculan dos puntos medios tipo Donchian en el marco temporal seleccionado:
   - El **punto medio rápido** promedia el máximo más alto y el mínimo más bajo en `UpPeriod1` y `DownPeriod1` barras.
   - El **punto medio lento** realiza la misma operación con `UpPeriod2` y `DownPeriod2` barras.
2. Cada punto medio es suavizado por una media móvil (`Method1`, `Method2`) de longitudes `XLength1` y `XLength2`. Los métodos de suavizado disponibles son Simple, Exponencial, Suavizado y Ponderado Lineal.
3. El valor del oscilador es la diferencia entre los dos puntos medios suavizados. Cuatro estados de color describen su comportamiento:
   - `PositiveRising` (0): el oscilador está por encima de cero y sube.
   - `PositiveFalling` (1): el oscilador está por encima de cero y pierde impulso.
   - `NegativeRising` (3): el oscilador está por debajo de cero pero sube hacia cero.
   - `NegativeFalling` (4): el oscilador está por debajo de cero y cae más.
   - `Neutral` (2) se asigna durante el calentamiento.
4. Las señales se evalúan usando los colores de la barra en `SignalBar` y la barra inmediatamente anterior (`SignalBar + 1`), lo que refleja el desplazamiento de búfer en la versión MQL.

## Lógica de trading

- **Entrada larga**: permitida cuando `EnableBuyOpen` es verdadero. Si el color de la barra más antigua (`SignalBar + 1`) era ascendente (0 o 3) y la barra más reciente (`SignalBar`) cambió a un color descendente (1 o 4), la estrategia cierra cualquier posición corta (`EnableSellClose`) y abre/extiende una posición larga usando `Volume + |Position|` unidades.
- **Entrada corta**: permitida cuando `EnableSellOpen` es verdadero. Si el color de la barra más antigua era descendente (1 o 4) y la barra más reciente cambió a un color ascendente (0 o 3), la estrategia cierra los largos existentes (`EnableBuyClose`) y abre/extiende una posición corta con `Volume + |Position|` unidades.
- Todas las ejecuciones ocurren al cierre de la vela que genera la señal. Las órdenes son siempre de mercado y la estrategia no aplica niveles adicionales de stop-loss o take-profit; depende únicamente de las transiciones de color para las salidas.
- `StartProtection()` se activa al inicio para usar las verificaciones de seguridad integradas del framework para posiciones inesperadas.

## Parámetros

| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `CandleType` | Marco temporal utilizado para los cálculos del indicador. | Velas de 4 horas |
| `UpPeriod1`, `DownPeriod1` | Ventanas de retrospectiva para el punto medio rápido. | 6, 6 |
| `UpPeriod2`, `DownPeriod2` | Ventanas de retrospectiva para el punto medio lento. | 9, 9 |
| `XLength1`, `XLength2` | Longitudes de suavizado para las dos medias móviles. | 25, 80 |
| `Method1`, `Method2` | Tipos de media móvil (Simple, Exponencial, Suavizado, Ponderado). | Simple |
| `SignalBar` | Desplazamiento de barra histórica usado para leer colores del oscilador. | 1 |
| `EnableBuyOpen`, `EnableSellOpen` | Activar entradas largas/cortas. | true |
| `EnableBuyClose`, `EnableSellClose` | Activar salidas largas/cortas. | true |
| `Volume` | Tamaño base de operación; las posiciones existentes se suman a este valor al revertir. | 1 |

## Notas de uso

- Los tipos de media móvil cubren los comportamientos de suavizado más comunes del experto original. Las opciones avanzadas como los ajustes de fase XMA personalizados no están disponibles en StockSharp y fueron reemplazadas con indicadores estándar.
- Dado que el oscilador se calcula en velas cerradas, las señales aparecen con el mismo retraso de una barra que usaba la implementación MQL (`SignalBar = 1`). Aumente `SignalBar` si necesita barras de confirmación adicionales.
- Considere combinar la estrategia con gestión de riesgo externa (gestor de cartera, stops protectores) cuando opere en mercados en vivo, ya que las salidas dependen exclusivamente de las reversiones de color del oscilador.
