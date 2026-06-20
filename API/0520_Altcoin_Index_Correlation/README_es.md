# Estrategia de Correlación con el Índice de Altcoins
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia compara las tendencias de EMA del instrumento operado y un índice de referencia. Abre largo cuando ambas EMA rápidas están por encima de sus EMA lentas, y corto cuando ambas están por debajo. La lógica inversa opcional permite operar contra la tendencia del índice o ignorar el índice completamente.

## Detalles

- **Criterios de entrada**:
  - EMA rápida por encima de la EMA lenta en ambos instrumentos (o lo contrario si es inverso).
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Condición de cruce opuesto.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `FastEmaLength` = 47
  - `SlowEmaLength` = 50
  - `IndexFastEmaLength` = 47
  - `IndexSlowEmaLength` = 50
  - `SkipIndexReference` = false
  - `InverseSignal` = false
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: EMA
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
