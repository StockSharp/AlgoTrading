# Estrategia Cazadora de Reversiones
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Reversal Catcher entra cuando el precio supera una banda de Bollinger y luego regresa mientras el impulso cambia. Se basa en una EMA rápida y lenta para definir la dirección de la tendencia y utiliza cruces del RSI en niveles de sobrecompra o sobreventa para sincronizar las entradas. Los objetivos y los stops se derivan de los niveles de las bandas de Bollinger y el extremo de la vela anterior. Las posiciones pueden cerrarse opcionalmente a una hora de fin de día especificada.

## Detalles

- **Criterios de entrada**: El precio vuelve a entrar en las Bandas de Bollinger con estructura de máximos/mínimos más altos y RSI cruzando extremos.
- **Largo/Corto**: Ambos
- **Criterios de salida**: Stop-loss, objetivo o cierre al final del día
- **Stops**: Extremo de la vela anterior
- **Valores predeterminados**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 1.5
  - `FastEmaPeriod` = 21
  - `SlowEmaPeriod` = 50
  - `RsiPeriod` = 14
  - `Overbought` = 70
  - `Oversold` = 30
  - `EndOfDay` = 1500
  - `CandleType` = 5 minutos
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: Bollinger Bands, EMA, RSI
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
