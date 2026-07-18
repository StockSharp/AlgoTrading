# Estrategia de Seguimiento del Alligator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementación de la estrategia Ride Alligator. El método utiliza tres medias móviles conocidas como el indicador Alligator. Se abre una posición larga cuando la línea Lips cruza por encima de la línea Jaws mientras la línea Teeth está por debajo de Jaws. Se abre una posición corta cuando Lips cruza por debajo de Jaws y la línea Teeth está por encima de Jaws. La posición abierta está protegida por un stop en la línea Jaws que sigue el movimiento de la línea.

## Detalles

- **Criterios de entrada**:
  - Largo: `Lips > Jaws && Teeth < Jaws && previous Lips < previous Jaws`
  - Corto: `Lips < Jaws && Teeth > Jaws && previous Lips > previous Jaws`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Largo: `price <= Jaws`
  - Corto: `price >= Jaws`
- **Stops**: Trailing stop en Alligator Jaws
- **Valores predeterminados**:
  - `AlligatorPeriod` = 5
  - `MaType` = MovingAverageTypeEnum.Weighted
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Alligator
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
