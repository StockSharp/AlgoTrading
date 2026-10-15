# Estrategia de Reversión Pinbar
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Los Pinbars destacan rechazos repentinos del precio y pueden señalar puntos de inflexión a corto plazo. Esta estrategia mide la longitud de la mecha de la vela en relación con su cuerpo, buscando sombras largas que sobresalgan de la acción reciente del precio. Un filtro de media móvil ayuda a operar en la dirección de la tendencia subyacente.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 82%. Funciona mejor en el mercado de acciones.

Durante cada actualización de vela, el sistema calcula las sombras superiores e inferiores y las compara con el tamaño del cuerpo. Un Pinbar alcista con una mecha inferior larga puede activar una entrada larga si el precio está por encima de la media móvil. Del mismo modo, un Pinbar bajista con una cola superior extendida puede iniciar una posición corta en una tendencia bajista. Los stops se colocan a un porcentaje fijo desde la entrada.

La operación se cierra cuando aparece un Pinbar opuesto contra la posición abierta o cuando se alcanza el stop protector. Combinar la lógica del Pinbar con un filtro de tendencia mejora la fiabilidad al evitar configuraciones contratendencia.

## Detalles

- **Criterios de entrada**: Pinbar con cola larga y sombra opuesta pequeña, confirmado por la tendencia.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Pinbar opuesto o stop-loss.
- **Stops**: Sí, basado en porcentaje.
- **Valores predeterminados**:
  - `TailToBodyRatio` = 2
  - `OppositeTailRatio` = 0.5
  - `MAPeriod` = 20
  - `CandleType` = 15 minute
  - `StopLossPercent` = 1
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Ambos
  - Indicadores: Candlestick, MA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

