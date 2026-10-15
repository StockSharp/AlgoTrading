# Estrategia de MACD Hidden Markov Model
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **MACD Hidden Markov Model** se basa en el MACD Hidden Markov Model.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 61%. Funciona mejor en el mercado de criptomonedas.

Las señales se activan cuando Markov confirma cambios de tendencia en datos intradía (5m). Este método es adecuado para operadores activos.

Los stops se basan en múltiplos de ATR y factores como MacdFast, MacdSlow. Ajuste estos valores predeterminados para equilibrar el riesgo y la recompensa.

## Detalles
- **Criterios de entrada**: ver implementación para condiciones de indicadores.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: señal opuesta o lógica de stop.
- **Stops**: Sí, usando cálculos basados en indicadores.
- **Valores predeterminados**:
  - `MacdFast = 12`
  - `MacdSlow = 26`
  - `MacdSignal = 9`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
  - `HmmHistoryLength = 100`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Markov
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: Sí
  - Divergencia: No
  - Nivel de riesgo: Medio
