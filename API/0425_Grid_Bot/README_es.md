# Estrategia Grid Bot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El grid bot divide un rango de precios predefinido en niveles iguales y opera las oscilaciones entre ellos. Cuando el precio se desplaza hacia la mitad inferior de la cuadrícula, la estrategia acumula posiciones largas, vendiéndolas cuando el precio regresa a la mitad superior. Este enfoque prospera en mercados laterales con límites claros.

No se asume ningún sesgo direccional; el bot simplemente reacciona a la proximidad a las líneas de la cuadrícula.

## Detalles

- **Criterios de entrada**:
  - **Largo**: el precio toca un nivel en la mitad inferior sin posición larga
  - **Corto**: el precio toca un nivel en la mitad superior sin posición corta
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - La señal de entrada opuesta cierra la posición existente
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `UpperLimit` = 48000
  - `LowerLimit` = 45000
  - `GridCount` = 10
- **Filtros**:
  - Categoría: Range trading
  - Dirección: Ambos
  - Indicadores: Price levels
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
