# Estrategia de Oscilador CG Adaptativo X2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Utiliza el Oscilador CG Adaptativo en dos marcos temporales diferentes.
El marco temporal superior define la tendencia predominante mientras que el inferior
gestiona las entradas y salidas reales basadas en los cruces del oscilador.

## Detalles

- **Criterios de entrada**:
  - Largo: el oscilador cruza por debajo de su línea de señal mientras la tendencia global es alcista
  - Corto: el oscilador cruza por encima de su línea de señal mientras la tendencia global es bajista
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal opuesta o indicadores de cierre explícito
- **Stops**: No
- **Valores predeterminados**:
  - `TrendAlpha` = 0.07m
  - `SignalAlpha` = 0.07m
  - `TrendCandleType` = TimeSpan.FromHours(6).TimeFrame()
  - `SignalCandleType` = TimeSpan.FromMinutes(30).TimeFrame()
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: Adaptive CG Oscillator
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Multi-timeframe
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
