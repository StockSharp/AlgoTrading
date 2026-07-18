# Estrategia de Squeeze Pro Overlays
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Squeeze Pro Overlays detecta la contracción de volatilidad cuando las Bandas de Bollinger se encuentran completamente dentro de múltiples Canales de Keltner. Una vez que el squeeze se libera, la pendiente de una regresión lineal sobre los precios de cierre determina la dirección de la operación.

## Detalles

- **Criterios de entrada**:
  - El squeeze termina (las Bandas de Bollinger se mueven fuera del Canal de Keltner más amplio).
  - **Largo**: Pendiente de momentum > 0.
  - **Corto**: Pendiente de momentum < 0.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Señal opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `SqueezeLength` = 20
- **Filtros**:
  - Categoría: Ruptura de volatilidad
  - Dirección: Ambos
  - Indicadores: Bollinger Bands, Keltner Channels, Linear Regression
  - Stops: Ninguno
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
