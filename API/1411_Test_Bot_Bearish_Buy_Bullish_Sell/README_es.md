# Bot de Prueba: Compra en Bajista / Venta en Alcista
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Entra largo en la primera vela bajista y sale en la primera vela alcista.

## Detalles

- **Criterios de entrada**: Primera vela bajista cuando está sin posición.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Primera vela alcista.
- **Stops**: No.
- **Valores predeterminados**:
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Largo
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
