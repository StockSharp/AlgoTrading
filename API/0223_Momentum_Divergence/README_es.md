# Estrategia de Divergencia de Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Divergencia de Momentum compara las lecturas de momentum con la dirección del precio para detectar señales tempranas de una reversión. Las divergencias ocurren cuando el precio alcanza un nuevo extremo pero el indicador de momentum no lo confirma, sugiriendo un debilitamiento de la fuerza.

Las pruebas indican un retorno anual promedio de aproximadamente 106%. Funciona mejor en el mercado de acciones.

Una configuración alcista ocurre cuando el precio registra un mínimo más bajo mientras el oscilador de momentum imprime un mínimo más alto. Una configuración bajista se forma cuando el precio empuja a un máximo más alto pero el momentum no sigue. Las posiciones se cierran cuando el momentum cruza de vuelta a través de cero o la divergencia se invalida.

Este enfoque atrae a traders que buscan anticipar puntos de inflexión en lugar de seguir tendencias. Los stops se utilizan para controlar el riesgo en caso de que el mercado continúe moviéndose en contra de la señal de divergencia.

## Detalles
- **Criterios de entrada**:
  - **Largo**: El precio hace un mínimo más bajo && El Momentum muestra un mínimo más alto
  - **Corto**: El precio hace un máximo más alto && El Momentum muestra un máximo más bajo
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir cuando el momentum cruza por debajo de cero
  - **Corto**: Salir cuando el momentum cruza por encima de cero
- **Stops**: Sí, stop-loss fijo.
- **Valores predeterminados**:
  - `MomentumPeriod` = 14
  - `MaPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: Momentum
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio
