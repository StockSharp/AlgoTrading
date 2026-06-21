# Estrategia de Color
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que opera según el brillo percibido de un color configurado.
Si el color es claro (luminancia > 0.5) la estrategia compra, de lo contrario vende.

## Detalles

- **Criterios de entrada**:
  - Largo: `Color luminance > 0.5`
  - Corto: `Color luminance <= 0.5`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal opuesta
- **Stops**: No
- **Valores predeterminados**:
  - `ColorHex` = "#f23645"
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoría: Otro
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
