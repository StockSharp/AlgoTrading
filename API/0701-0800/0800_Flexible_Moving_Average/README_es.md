# Estrategia de Media Móvil Flexible
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ajusta la posición según los cruces entre el cierre del período anterior y una media móvil configurable. Un cruce a la baja reduce la posición en un porcentaje definido por el usuario, mientras que un cruce al alza restaura la posición completa.

## Detalles

- **Criterios de entrada**:
  - **Inicial**: Largo completo opcional en la primera barra.
  - **Incremento**: El cierre anterior cruza por encima de la media móvil → posición al 100%.
- **Criterios de salida**:
  - **Reducción**: El cierre anterior cruza por debajo de la media móvil → reducir en `SellPercentage`.
- **Indicadores**:
  - Media móvil simple, exponencial, ponderada, Hull o suavizada.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `MaLength` = 200
  - `SellPercentage` = 100
  - `MaMethod` = SMA
  - `AllowInitialBuy` = true
- **Filtros**:
  - Seguimiento de tendencia
  - Marco temporal único
  - Indicadores: medias móviles
  - Stops: ninguno
  - Complejidad: Básico

