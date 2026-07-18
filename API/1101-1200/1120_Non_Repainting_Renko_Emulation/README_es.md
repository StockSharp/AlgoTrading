# Estrategia de Emulación Renko sin Repintado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Emula ladrillos Renko usando precios de cierre y opera en transiciones de patrón sin repintado.

## Detalles

- **Criterios de entrada**:
  - Después de que se forma un nuevo ladrillo, ir largo cuando la dirección del ladrillo anterior y la secuencia de precios muestran continuación alcista.
  - Ir corto en la secuencia inversa.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Cerrar posiciones cuando la dirección del ladrillo se invierte.
- **Stops**: No.
- **Valores predeterminados**:
  - `BrickSize` = 3
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
