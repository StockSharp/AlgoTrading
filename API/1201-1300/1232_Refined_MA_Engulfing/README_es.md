# MA Refinada + Envolvente (M5 + Ruptura de Estructura Confirmada)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

MA Refinada + Envolvente combina dos medias móviles simples, velas envolventes y confirmación de ruptura de estructura. Se coloca una operación cuando al menos dos factores de confluencia se alinean y ha pasado el período de enfriamiento.

## Detalles

- **Criterios de entrada**: Después de una ruptura de estructura alcista o bajista confirmada, precio por encima o por debajo de ambas SMA, y al menos dos de cuatro confluencias (envolvente, ruptura de estructura, filtro MA, marcador fib) con el enfriamiento satisfecho.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Ninguno.
- **Stops**: No.
- **Valores predeterminados**:
  - `Ma1Length` = 66
  - `Ma2Length` = 85
  - `CooldownBars` = 5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Ambos
  - Indicadores: SMA, Engulfing, Structure Break
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: 5-minute
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
