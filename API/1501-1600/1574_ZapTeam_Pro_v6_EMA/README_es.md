# Estrategia ZapTeam Pro v6 — EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Versión simplificada que usa el cruce de EMA21/EMA50 con filtro de tendencia EMA200. Compra en cruce alcista y vende en cruce bajista (cortos opcionales).

## Detalles

- **Criterios de entrada**: EMA21 cruza EMA50 con filtro de tendencia
- **Largo/Corto**: Ambos (cortos opcionales)
- **Criterios de salida**: Cruce contrario
- **Stops**: No
- **Valores predeterminados**:
  - `Ema21Length` = 21
  - `Ema50Length` = 50
  - `Ema200Length` = 200
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: EMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
