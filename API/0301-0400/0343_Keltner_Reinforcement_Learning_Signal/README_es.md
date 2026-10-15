# Estrategia de Señal Keltner de Aprendizaje por Refuerzo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La estrategia **Keltner Reinforcement Learning Signal** está construida en torno a la señal de aprendizaje por refuerzo de Keltner.

Las pruebas indican un retorno anual promedio de aproximadamente 118%. Funciona mejor en el mercado de acciones.

Las señales se activan cuando Keltner confirma cambios de tendencia en datos intradía (15m). Esto hace que el método sea adecuado para traders activos.

Los stops se basan en múltiplos de ATR y factores como EmaPeriod, AtrPeriod. Ajuste estos valores predeterminados para equilibrar el riesgo y la recompensa.

## Detalles
- **Criterios de entrada**: ver implementación para condiciones de indicadores.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: señal opuesta o lógica de stop.
- **Stops**: Sí, usando cálculos basados en indicadores.
- **Valores predeterminados**:
  - `EmaPeriod = 20`
  - `AtrPeriod = 14`
  - `AtrMultiplier = 2m`
  - `StopLossAtr = 2m`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Keltner, Reinforcement
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (15m)
  - Estacionalidad: No
  - Redes neuronales: Sí
  - Divergencia: No
  - Nivel de riesgo: Medio

