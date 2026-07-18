# Canales Keltner Duales
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de **Canales Keltner Duales** utiliza dos canales Keltner con diferentes multiplicadores para detectar rupturas.
Se abre una operación cuando el precio perfora la banda exterior y luego regresa a través de la banda interior.
Los stops y objetivos se gestionan con porcentajes fijos.

## Detalles
- **Criterios de entrada**: El precio cruza la banda exterior de Keltner y vuelve a cruzar la banda interior en la misma dirección.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Stop loss, take profit o señal opuesta.
- **Stops**: Sí, basados en porcentaje.
- **Valores predeterminados**:
  - `EmaPeriod = 50`
  - `InnerMultiplier = 2.75m`
  - `OuterMultiplier = 3.75m`
  - `MaxStopPercent = 10m`
  - `SlTpRatio = 1m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Keltner
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
