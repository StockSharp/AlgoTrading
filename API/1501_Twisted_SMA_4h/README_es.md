# Estrategia Twisted SMA 4h
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia Twisted SMA usa tres medias móviles simples y un filtro KAMA en velas de 4 horas. Se abre una posición larga cuando la SMA rápida está por encima de la media, la media por encima de la lenta, el precio por encima de una SMA más larga y la KAMA no está plana. La posición se cierra cuando las SMA se alinean de forma bajista.

## Detalles

- **Criterios de entrada**: SMA rápida > SMA media > SMA lenta, cierre > SMA principal, KAMA no plana.
- **Largo/Corto**: Solo largo.
- **Criterios de salida**: SMA rápida < SMA media < SMA lenta.
- **Stops**: No.
- **Valores predeterminados**:
  - `FastLength` = 4
  - `MidLength` = 9
  - `SlowLength` = 18
  - `MainSmaLength` = 100
  - `KamaLength` = 25
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Largo
  - Indicadores: SMA, KAMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
