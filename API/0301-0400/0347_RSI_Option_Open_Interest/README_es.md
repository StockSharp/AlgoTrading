# Estrategia RSI de Interés Abierto en Opciones
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La estrategia **RSI Option Open Interest** está construida en torno al interés abierto en opciones del RSI.

Las pruebas indican un retorno anual promedio de aproximadamente 130%. Funciona mejor en el mercado de acciones.

Las señales se activan cuando Option confirma cambios de tendencia en datos intradía (5m). Esto hace que el método sea adecuado para traders activos.

Los stops se basan en múltiplos de ATR y factores como RsiPeriod, CandleType. Ajuste estos valores predeterminados para equilibrar el riesgo y la recompensa.

## Detalles
- **Criterios de entrada**: ver implementación para condiciones de indicadores.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: señal opuesta o lógica de stop.
- **Stops**: Sí, usando cálculos basados en indicadores.
- **Valores predeterminados**:
  - `RsiPeriod = 14`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
  - `OiPeriod = 20`
  - `OiDeviationFactor = 2m`
  - `StopLoss = 2m`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Option, Open, Interest
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

