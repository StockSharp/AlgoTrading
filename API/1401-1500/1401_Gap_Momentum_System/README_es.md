# Sistema de Momentum de Gaps (Estrategia)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementa el sistema de momentum de gaps de Perry Kaufman. La estrategia compara los gaps acumulados al alza y a la baja, y opera cuando la señal sube o baja.

## Detalles
- **Criterios de entrada**: Señal en alza -> comprar, señal en baja -> vender o revertir.
- **Largo/Corto**: Configurable.
- **Criterios de salida**: Señal opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Period` = 40
  - `SignalPeriod` = 20
  - `LongOnly` = true
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos o solo largos
  - Indicadores: Gap momentum
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
