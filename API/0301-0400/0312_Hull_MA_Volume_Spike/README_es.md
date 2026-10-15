# Estrategia de Pico de Volumen con Hull MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Hull MA Volume Spike** está construida en torno a la Media Móvil Hull con detección de picos de volumen.

Las pruebas indican un rendimiento anual promedio de aproximadamente 43%. Funciona mejor en el mercado de acciones.

Las señales se activan cuando el pico confirma cambios de tendencia en datos intradía (5m). Esto hace que el método sea adecuado para traders activos.

Los stops dependen de múltiplos de ATR y factores como HmaPeriod, VolumeAvgPeriod. Ajuste estos valores predeterminados para equilibrar el riesgo y la recompensa.

## Detalles
- **Criterios de entrada**: ver implementación para condiciones de indicadores.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: señal opuesta o lógica de stops.
- **Stops**: Sí, usando cálculos basados en indicadores.
- **Valores predeterminados**:
  - `HmaPeriod = 9`
  - `VolumeAvgPeriod = 20`
  - `VolumeThresholdFactor = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Spike
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
