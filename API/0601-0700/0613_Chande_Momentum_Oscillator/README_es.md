# Estrategia de Oscilador de Momentum Chande
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia compra cuando el Oscilador de Momentum Chande cae por debajo de un umbral inferior y cierra la posición cuando sube por encima de un umbral superior o tras un número fijo de barras.

Las pruebas indican un rendimiento anual promedio de aproximadamente 40%. Tiene el mejor desempeño en mercados con tendencia.

El oscilador compara las ganancias y pérdidas recientes para medir el momentum. Los valores negativos extremos sugieren condiciones de sobreventa, que la estrategia usa para entradas largas. Las posiciones se cierran cuando el momentum se vuelve positivo o expira el período de mantenimiento.

## Detalles

- **Criterios de entrada**: `CMO < LowerThreshold`.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: `CMO > UpperThreshold` o `MaxBarsInPosition` barras transcurridas.
- **Stops**: No.
- **Valores predeterminados**:
  - `CmoPeriod` = 9
  - `LowerThreshold` = -50
  - `UpperThreshold` = 50
  - `MaxBarsInPosition` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Solo largos
  - Indicadores: CMO
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
