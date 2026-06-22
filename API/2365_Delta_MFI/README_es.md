# Estrategia Delta MFI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en la comparación de los valores rápido y lento del Índice de Flujo de Dinero (MFI). Va largo cuando el MFI rápido sube por encima del MFI lento mientras el MFI lento está por encima del nivel de señal. Va corto cuando el MFI rápido cae por debajo del MFI lento mientras el MFI lento está por debajo de 100 menos el nivel de señal.

## Detalles

- **Criterios de entrada**: 
  - Comprar cuando `slow MFI > Level` y `fast MFI > slow MFI`
  - Vender cuando `slow MFI < 100 - Level` y `fast MFI < slow MFI`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal opuesta
- **Stops**: No
- **Valores predeterminados**:
  - `FastPeriod` = 14
  - `SlowPeriod` = 50
  - `Level` = 50
  - `CandleType` = velas de 4 horas
- **Filtros**:
  - Categoría: Indicador
  - Dirección: Ambos
  - Indicadores: Money Flow Index
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: H4
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
