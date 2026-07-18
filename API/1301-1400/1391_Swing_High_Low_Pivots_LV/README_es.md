# Estrategia de Pivotes de Máximos y Mínimos de Swing [LV]
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Opera alrededor de máximos y mínimos de swing confirmados. Cuando aparece un pivote mínimo, la estrategia coloca una orden limitada de compra en el precio de la barra del pivote y establece objetivos fijos de stop y take-profit. Los pivotes máximos activan configuraciones cortas. Un filtro de media móvil opcional puede restringir las operaciones a la dirección de la tendencia.

## Detalles

- **Entradas**:
  - Longitud del pivote.
  - Distancia de stop-loss en ticks.
  - Distancia de take-profit en ticks.
  - Segundo take-profit y switch de entrada doble.
  - Tipo y longitud del filtro de media móvil.
- **Largo/Corto**: Ambos.
- **Salida**: Stop fijo y hasta dos objetivos de beneficio.
- **Filtros**:
  - Categoría: Reconocimiento de patrones
  - Dirección: Ambos
  - Indicadores: Media móvil
  - Stops: Fijo
  - Complejidad: Alto
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
