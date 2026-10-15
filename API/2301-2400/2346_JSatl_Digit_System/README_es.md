# JSatl Sistema Digit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El JSatl Sistema Digit utiliza una Media Móvil Jurik (JMA) para determinar la dirección de la tendencia.
La estrategia mide la pendiente de la JMA y abre una posición cuando el precio confirma la dirección de la pendiente.

Se abre una posición larga cuando la JMA está subiendo y el precio de cierre está por encima de la media.
Se abre una posición corta cuando la JMA está bajando y el precio de cierre está por debajo de la media.
Las señales opuestas cierran cualquier posición abierta.

## Detalles

- **Criterios de entrada**: Pendiente de la JMA con confirmación de precio.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `JmaLength` = 14
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: JMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Swing (4h)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
