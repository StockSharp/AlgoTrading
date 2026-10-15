# Estrategia de Price Radio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa el indicador Price Radio de John Ehlers. Entra en largo cuando la derivada del precio supera tanto el umbral de amplitud como el de frecuencia, y entra en corto cuando cae por debajo de sus valores negativos.

## Detalles

- **Criterios de entrada**:
  - **Largo**: la derivada es mayor que la amplitud y la frecuencia.
  - **Corto**: la derivada es menor que la amplitud negativa y la frecuencia negativa.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `Length` = 14.
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: Custom
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
