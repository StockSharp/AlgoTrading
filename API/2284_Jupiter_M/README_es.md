# Estrategia Jupiter M
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de grilla traducida del experto MetaTrader "Jupiter M. 4.1.1".
El algoritmo construye una cesta de órdenes con un paso configurable y adapta
tanto el take profit como el volumen a medida que se abren nuevos niveles.

## Detalles

- **Criterios de entrada**:
  - Largo: el precio cae por el tamaño del paso y (opcional) CCI < -100
  - Corto: el precio sube por el tamaño del paso y (opcional) CCI > 100
- **Largo/Corto**: Ambos
- **Criterios de salida**: La cesta alcanza el take profit calculado
- **Stops**: Punto de equilibrio después de un número especificado de pasos
- **Valores predeterminados**:
  - `TakeProfit` = 10
  - `FirstStep` = 20
  - `FirstVolume` = 0.01
  - `VolumeMultiplier` = 2
  - `CciPeriod` = 50
  - `CandleType` = velas de 5 minutos
- **Filtros**:
  - Categoría: Grilla, reversión a la media
  - Dirección: Ambos
  - Indicadores: CCI (opcional)
  - Stops: Punto de equilibrio
  - Complejidad: Avanzado
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto

## Parámetros

- `TakeProfit` – objetivo de ganancia en unidades de precio para la cesta.
- `UseAverageTakeProfit` – calcular el take profit desde el precio promedio de las órdenes abiertas.
- `DynamicTakeProfit` – reducir el take profit después de `TpDynamicStep` usando `TpDecreaseFactor` con un mínimo en `MinTakeProfit`.
- `BreakevenClose` / `BreakevenStep` – mover el objetivo al punto de equilibrio después de un número de pasos.
- `FirstStep` – distancia inicial entre niveles de la grilla.
- `DynamicStep`, `StepIncreaseStep`, `StepIncreaseFactor` – aumentar el paso para cada orden adicional.
- `MaxStepsBuy` / `MaxStepsSell` – número máximo de órdenes por dirección.
- `FirstVolume`, `VolumeMultiplier`, `MultiplyUseStep` – controlan el crecimiento del volumen en la grilla.
- `CciFilter` / `CciPeriod` – filtro opcional por CCI para la primera orden.
- `AllowBuy` / `AllowSell` – habilitar direcciones de trading.
- `CandleType` – marco temporal de las velas para los cálculos.

La estrategia busca capturar la reversión a la media del precio promediando posiciones
y cerrando la cesta en objetivos de ganancia dinámicos.
