# Estrategia Exp KWAN NRP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Exp KWAN NRP reproduce el asesor experto original de MetaTrader combinando un oscilador estocástico, índice de fuerza relativa e indicador de momentum en una sola razón. La razón se suaviza con una media móvil configurable, y la pendiente de la línea suavizada determina cuándo abrir o cerrar posiciones. El enfoque funciona en cualquier símbolo o marco temporal y está diseñado para el trading direccional cuando el momentum cambia.

## Lógica de trading
1. Construir la razón KWAN multiplicando la línea %D del estocástico por el valor RSI y dividiendo por la lectura del momentum.
2. Suavizar la razón con el método de media móvil seleccionado (simple, exponencial, suavizada o ponderada).
3. Evaluar la pendiente de la línea suavizada en el desplazamiento de barra de señal configurable.
4. Entrar en posiciones largas cuando la línea gira hacia arriba y salir de posiciones cortas. Entrar en posiciones cortas cuando la línea gira hacia abajo y salir de posiciones largas.
5. La protección opcional de stop-loss y take-profit puede cerrar automáticamente posiciones después de un movimiento de precio predefinido medido en pasos de precio.

## Señales
- **Entrada larga**: El valor KWAN suavizado en la barra de señal sube comparado con la barra anterior y las entradas largas están habilitadas.
- **Salida larga**: El valor KWAN suavizado gira hacia abajo mientras una posición larga está abierta y las salidas largas están habilitadas.
- **Entrada corta**: El valor KWAN suavizado en la barra de señal cae comparado con la barra anterior y las entradas cortas están habilitadas.
- **Salida corta**: El valor KWAN suavizado gira hacia arriba mientras una posición corta está abierta y las salidas cortas están habilitadas.

## Gestión del riesgo
- Establezca la propiedad `Volume` de la estrategia para controlar el tamaño base de la orden. El cambio de posición cierra automáticamente una posición opuesta antes de abrir una nueva.
- Habilite `UseProtection` para aplicar niveles de stop-loss y take-profit medidos en pasos de precio del instrumento. Ambas protecciones se pueden usar juntas o por separado.
- La estrategia se suscribe a velas definidas por `CandleType` y opera al cierre de velas finalizadas.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `CandleType` | Marco temporal usado para cálculos de indicadores y evaluación de señales. | Velas de 1 hora |
| `KPeriod` | Período de la línea %K del estocástico. | 5 |
| `DPeriod` | Período de la línea %D del estocástico. | 3 |
| `SlowingPeriod` | Suavizado adicional aplicado a la línea %K del estocástico. | 3 |
| `RsiPeriod` | Período del índice de fuerza relativa. | 14 |
| `MomentumPeriod` | Período del indicador de momentum. | 14 |
| `SmoothingMethod` | Tipo de media móvil aplicada a la razón KWAN (Simple, Exponential, Smoothed, Weighted). | Simple |
| `SmoothingLength` | Longitud de la media móvil de suavizado. | 3 |
| `SignalBar` | Número de barras atrás usado para evaluar la pendiente (0 = barra cerrada actual). | 1 |
| `EnableBuyEntries` | Permitir abrir posiciones largas en señales alcistas. | true |
| `EnableSellEntries` | Permitir abrir posiciones cortas en señales bajistas. | true |
| `EnableBuyExits` | Permitir cerrar posiciones largas cuando aparece una señal bajista. | true |
| `EnableSellExits` | Permitir cerrar posiciones cortas cuando aparece una señal alcista. | true |
| `UseProtection` | Habilitar protecciones de stop-loss y take-profit. | true |
| `StopLossSteps` | Distancia del stop-loss expresada en pasos de precio. | 1000 |
| `TakeProfitSteps` | Distancia del take-profit expresada en pasos de precio. | 2000 |

## Notas de uso
- La razón KWAN puede volverse inestable cuando el indicador de momentum es igual a cero. La estrategia omite automáticamente las señales para esas barras para evitar la división por cero.
- El parámetro `SignalBar` permite alinear señales con barras históricas si se necesita confirmación retrasada.
- Combine con controles de riesgo a nivel de corretaje o filtros adicionales si se requiere para trading en producción.
