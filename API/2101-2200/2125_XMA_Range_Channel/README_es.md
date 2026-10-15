# Estrategia de Canal de Rango XMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que construye un canal superior e inferior a partir de medias móviles de los precios máximos y mínimos. Una ruptura por encima de la banda superior activa una entrada larga, mientras que una ruptura por debajo de la banda inferior activa una entrada corta. El modelo replica el comportamiento del experto MQL original "XMA Range Channel".

## Detalles

- **Criterios de entrada**:
  - Largo: `Close > UpperChannel`
  - Corto: `Close < LowerChannel`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal opuesta
- **Stops**: No
- **Valores predeterminados**:
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
  - `Length` = 7
- **Filtros**:
  - Categoría: Ruptura de canal
  - Dirección: Ambos
  - Indicadores: SMA en High/Low
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Swing
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
