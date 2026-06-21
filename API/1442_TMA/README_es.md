# Estrategia TMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia TMA utiliza múltiples medias móviles suavizadas y patrones de velas para operar en la dirección de la tendencia de 200 períodos. Combina señales de golpe de 3 líneas y envolvente con un filtro de sesión.

## Detalles

- **Criterios de entrada**: envolvente alcista o golpe de 3 líneas en tendencia alcista / envolvente bajista o golpe de 3 líneas en tendencia bajista con EMA(2) por encima/debajo de SMA(200) y filtro de sesión opcional
- **Largo/Corto**: Ambos
- **Criterios de salida**: EMA(2) cruzando SMA(200)
- **Stops**: No
- **Valores predeterminados**:
  - `CandleType` = velas de 5 minutos
  - `FastLength` = 21
  - `MidLength` = 50
  - `Mid2Length` = 100
  - `SlowLength` = 200
  - `UseSession` = false
  - `SessionStart` = 08:30
  - `SessionEnd` = 12:00
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: SMA, EMA
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
