# Oscilador de Gráfico
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera usando un oscilador seleccionable. Elige entre Estocástico, RSI o MFI. Compra cuando el oscilador señala condiciones de sobreventa y vende cuando está sobrecomprado. Para la opción Estocástico, las señales usan cruces de %K y %D.

Las pruebas muestran buen rendimiento en mercados volátiles como las criptomonedas.

Las posiciones se invierten cuando aparecen condiciones opuestas o se activa el stop-loss.

## Detalles

- **Criterios de entrada**: Niveles de sobreventa/sobrecompra del oscilador y cruces de %K/%D.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `Choice` = OscillatorChoice.Stochastic
  - `Length` = 14
  - `KPeriod` = 14
  - `DPeriod` = 3
  - `SmoothK` = 3
  - `Overbought` = 80
  - `Oversold` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLossPercent` = 2.0m
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: Stochastic/RSI/MFI
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
