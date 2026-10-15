# Estrategia ColorNonLagDot MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que usa el indicador MACD con varios modos de detección de señales. El enfoque está adaptado del asesor experto MQL "Exp_ColorNonLagDotMACD".

## Detalles

- **Criterios de entrada**: Depende del modo seleccionado (ruptura de línea cero, giro del MACD, giro de la línea de señal o cruce del MACD con la línea de señal).
- **Largo/Corto**: Ambas direcciones, se pueden habilitar por separado.
- **Criterios de salida**: Señales opuestas o stop/objetivo configurado.
- **Stops**: Stop-loss y take-profit opcionales basados en porcentaje.
- **Valores predeterminados**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `Mode` = `MacdDisposition`
  - `TakeProfitPercent` = 4
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: MACD
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: 4H
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
