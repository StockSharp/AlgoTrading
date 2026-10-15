# Estrategia de Exp Slow Stoch Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un port de alto nivel de StockSharp del asesor experto MetaTrader 5 **Exp_Slow-Stoch_Duplex**. Combina dos osciladores estocásticos lentos que funcionan en marcos temporales independientes para generar señales largas y cortas coordinadas. Cada oscilador entrega sus propias señales de cruce, permitiendo a la estrategia abrir o cerrar posiciones direccionales mientras las órdenes protectoras emulan la gestión original de stop-loss y take-profit.

## Reglas de trading

- **Módulo largo**
  - Evaluar el estocástico largo en el marco temporal `LongCandleType`.
  - Aplicar el método de suavizado configurado a los valores %K y %D y desplazarlos `LongSignalBar` barras.
  - Abrir una posición larga cuando %K cruza por encima de %D (`previousK <= previousD` y `currentK > currentD`).
  - Cerrar una posición larga existente cuando %K vuelve a bajar por debajo de %D (`currentK < currentD`).
- **Módulo corto**
  - Evaluar el estocástico corto en el marco temporal `ShortCandleType`.
  - Abrir una posición corta cuando %K cruza por debajo de %D (`previousK >= previousD` y `currentK < currentD`).
  - Cerrar una posición corta existente cuando %K vuelve a subir por encima de %D (`currentK > currentD`).
- Las órdenes se ejecutan con órdenes de mercado. El volumen enviado es igual a `TradeVolume` más el valor absoluto de la posición actual para que las reversiones aplanen primero la exposición anterior.
- Se adjuntan un take-profit y un stop-loss protectores en puntos de precio a través de `StartProtection` para imitar los parámetros de orden de MT5.

## Parámetros

| Parámetro | Tipo | Predeterminado | Descripción |
|-----------|------|----------------|-------------|
| `LongCandleType` | `DataType` | Velas de 8 horas | Marco temporal para el oscilador estocástico largo. |
| `LongKPeriod` | `int` | 5 | Período de cálculo %K para el estocástico largo. |
| `LongDPeriod` | `int` | 3 | Período de suavizado %D para el estocástico largo. |
| `LongSlowing` | `int` | 3 | Ralentización adicional aplicada dentro del cálculo estocástico. |
| `LongSignalBar` | `int` | 1 | Número de barras cerradas usadas para evaluar el cruce. |
| `LongSmoothingMethod` | `SmoothingMethod` | `Smoothed` | Suavizado secundario aplicado a %K y %D (None, Simple, Exponential, Smoothed, Weighted). |
| `LongSmoothingLength` | `int` | 5 | Longitud del filtro de suavizado secundario para el oscilador largo. |
| `LongEnableOpen` | `bool` | `true` | Permitir a la estrategia abrir posiciones largas. |
| `LongEnableClose` | `bool` | `true` | Permitir a la estrategia cerrar posiciones largas. |
| `ShortCandleType` | `DataType` | Velas de 8 horas | Marco temporal para el oscilador estocástico corto. |
| `ShortKPeriod` | `int` | 5 | Período de cálculo %K para el estocástico corto. |
| `ShortDPeriod` | `int` | 3 | Período de suavizado %D para el estocástico corto. |
| `ShortSlowing` | `int` | 3 | Ralentización adicional aplicada dentro del cálculo estocástico. |
| `ShortSignalBar` | `int` | 1 | Número de barras cerradas usadas para evaluar el cruce corto. |
| `ShortSmoothingMethod` | `SmoothingMethod` | `Smoothed` | Suavizado secundario aplicado a los valores cortos %K y %D. |
| `ShortSmoothingLength` | `int` | 5 | Longitud del filtro de suavizado secundario para el oscilador corto. |
| `ShortEnableOpen` | `bool` | `true` | Permitir a la estrategia abrir posiciones cortas. |
| `ShortEnableClose` | `bool` | `true` | Permitir a la estrategia cerrar posiciones cortas. |
| `TradeVolume` | `decimal` | 0.1 | Volumen base para entradas de posición. |
| `TakeProfitPoints` | `decimal` | 2000 | Distancia del take-profit expresada en puntos de precio. |
| `StopLossPoints` | `decimal` | 1000 | Distancia del stop-loss expresada en puntos de precio. |

## Notas

- El `SmoothingMethod` adicional imita el suavizado opcional basado en JJMA del indicador original usando las medias móviles estándar disponibles en StockSharp. Elija `None` para deshabilitar esta etapa si no se requiere replicación exacta.
- Los módulos largo y corto son independientes; puede habilitar o deshabilitar cualquier lado usando los flags booleanos correspondientes.
- Debido a que StockSharp opera con posiciones netas, la estrategia siempre cierra la exposición opuesta cuando una nueva señal invierte la dirección.
