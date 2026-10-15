# Estrategia FatlSatlOsma
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este ejemplo reproduce la lógica del experto de MetaTrader **Exp_FatlSatlOsma** usando la API de alto nivel de StockSharp.  
El sistema original trabaja con el oscilador Fatl/Satl (un indicador personalizado similar al MACD).  
La estrategia busca un cambio en la dirección del oscilador:

- Cuando el oscilador sube durante dos barras y el último valor es mayor que el anterior, se abre una posición larga y se cierran las posiciones cortas.
- Cuando el oscilador baja durante dos barras y el último valor es menor que el anterior, se abre una posición corta y se cierran las posiciones largas.

El oscilador se implementa a través del indicador integrado `MovingAverageConvergenceDivergenceSignal` con períodos rápidos y lentos configurables.  
Los valores predeterminados corresponden a los parámetros originales de FATL/SATL.

## Detalles

- **Criterios de entrada**: aceleración del oscilador.
- **Largo/Corto**: ambos.
- **Criterios de salida**: aceleración opuesta.
- **Stops**: ninguno.
- **Valores predeterminados**:
  - `Fast` = 39
  - `Slow` = 65
  - `CandleType` = marco temporal de 12 horas
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: MACD
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio
