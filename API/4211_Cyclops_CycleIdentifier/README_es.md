# Estrategia de identificador de ciclo cíclope
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia traslada el asesor experto de MetaTrader **Cyclops v1.2** junto con su indicador propietario *CycleIdentifier* al nivel alto de StockSharp API. El algoritmo suaviza los precios de cierre con un promedio móvil suavizado (SMMA), mide la volatilidad reciente a través de un rango verdadero promedio retrospectivo largo y marca puntos de inflexión del ciclo cuando el precio se aleja lo suficiente de la oscilación más reciente. Las reversiones de ciclo importantes generan nuevas entradas, mientras que las reversiones menores ofrecen señales de salida opcionales.

Un filtro de retraso cero configurable valida la pendiente de la serie suavizada. El filtro puede funcionar directamente en datos de precios suavizados o en un RSI estilo Wilder derivado de la misma serie. Hay confirmación adicional disponible a través de un indicador Momentum clásico, y las operaciones se pueden limitar a una ventana de día/hora específica.

## Lógica de señal

- **Detección de ciclo**: la máquina de estado interna rastrea los últimos máximos y mínimos del precio suavizado. Cuando el precio supera el umbral adaptativo (rango promedio × *Longitud*), la estrategia marca un ciclo menor. Se requiere un múltiplo mayor (*MajorCycleStrength*) para marcar un ciclo importante.
- **Entradas** – Los principales ciclos alcistas (`MajorBuy`) abren posiciones largas; Los principales ciclos bajistas (`MajorSell`) abren cortos. Las posiciones activas se cierran automáticamente antes de retroceder al lado opuesto.
- **Salidas opcionales**: cuando *UseExitSignal* está habilitado, las operaciones rentables pueden cerrarse en la señal de ciclo menor correspondiente (`MinorSellExit` para largos, `MinorBuyExit` para cortos) si no hay ningún ciclo principal opuesto presente.
- **Filtro de retardo cero**: si *UseCycleFilter* está habilitado, un filtro de suavizado de retardo cero debe confirmar la pendiente (aumentando para las posiciones largas, bajando para las posiciones cortas). La fuente del filtro se selecciona mediante *CycleFilterMode* (precio suavizado o RSI).
- **Filtro de impulso**: con *UseMomentumFilter* habilitado, las entradas requieren `Momentum ≥ MomentumTriggerLong` para largos y `Momentum ≤ MomentumTriggerShort` para cortos.

## Gestión comercial

- **Objetivos fijos** – *TakeProfitPips* y *StopLossPips* definen salidas fijas opcionales en pips de instrumentos.
- ** Punto de equilibrio **: cuando se alcanzan los pips de ganancia *BreakEvenTrigger*, el stop se coloca en la entrada ± un pip.
- **Trailing** – *TrailingStopTrigger* activa un trailing stop que sigue el precio en *TrailingStopPips* una vez que se alcanza la distancia de activación.
- **Control de sesión**: si *UseTimeRestriction* es verdadero, se permiten nuevas posiciones solo antes del `DayEnd` (0=domingo) y hasta el `HourEnd` (inclusive) de ese día. Las operaciones existentes se siguen gestionando posteriormente.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `Volume` | Volumen de pedidos utilizado para las entradas. |
| `PriceActionFilter` | Longitud de la media móvil suavizada aplicada al precio de cierre. |
| `Length` | Multiplicador aplicado al rango promedio para detectar ciclos menores. |
| `MajorCycleStrength` | Multiplicador que separa las oscilaciones mayores de las menores. |
| `UseCycleFilter` | Habilita la confirmación de pendiente con retraso cero. |
| `CycleFilterMode` | Selecciona entrada sin retraso: precio suavizado (`Sma`) o RSI (`Rsi`). |
| `FilterStrengthSma` | Longitud del filtro de retraso cero cuando se utiliza el precio suavizado. |
| `FilterStrengthRsi` | Duración y período RSI cuando el filtro se basa en valores RSI. |
| `UseMomentumFilter` | Activa o desactiva la confirmación de impulso. |
| `MomentumPeriod` | Longitud del indicador de momento. |
| `MomentumTriggerLong` | Impulso mínimo requerido para entradas largas. |
| `MomentumTriggerShort` | Máximo impulso permitido para entradas cortas. |
| `UseExitSignal` | Permite salidas basadas en ciclos menores cuando son rentables. |
| `UseTimeRestriction` | Limita el comercio a la ventana configurada de día/hora de la semana. |
| `DayEnd` | Último día de la semana en el que se permiten nuevas entradas. |
| `HourEnd` | Última hora del último día de negociación para nuevas entradas. |
| `BreakEvenTrigger` | Beneficio en pips necesarios para activar el punto de equilibrio. |
| `TrailingStopTrigger` | Beneficio en pips necesarios para comenzar a seguir. |
| `TrailingStopPips` | Distancia en pips mantenida por el trailing stop. |
| `TakeProfitPips` | Distancia de toma de ganancias fija en pips. |
| `StopLossPips` | Distancia fija de stop-loss en pips. |
| `CandleType` | Cronograma primario que alimenta la estrategia. |

## Diferencias respecto al EA original

- El rango promedio se estima con un rango verdadero promedio de 250 períodos multiplicado por *Longitud*, lo que proporciona un comportamiento equivalente al intervalo alto/bajo usado en MQL.
- La confirmación de impulso utiliza el valor real del indicador (el script MQL se compara con el multiplicador de pips `bm`, lo que desactiva efectivamente el filtro).
- El suavizado de retraso cero se implementa con los mismos coeficientes recursivos pero expresados en aritmética decimal. El modo RSI utiliza un Wilder RSI cuyo período es igual a *FilterStrengthRsi*.

## Notas de uso

1. Seleccione el instrumento y vincule el parámetro `CandleType` al período de tiempo deseado.
2. Configure los ajustes de riesgo y sesión para que coincidan con el entorno de su corredor.
3. Habilite *UseCycleFilter* o *UseMomentumFilter* cuando se requiera una confirmación más estricta; deshabilítelos para entradas más rápidas pero más ruidosas.
4. La estrategia mantiene como máximo una posición abierta. Las señales de ciclo opuesto cierran la posición actual antes de que se evalúe una nueva.
