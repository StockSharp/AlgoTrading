# Estrategia LANZ 3.0
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia LANZ 3.0 opera rupturas del rango asiático. La dirección se elige tras la ventana de decisión de 01:15–02:15 hora de Nueva York y se coloca una orden limitada en el máximo o mínimo del rango con objetivos y stops basados en Fibonacci. Si la orden no se ejecuta antes de las 02:15, puede invertir la dirección. Las órdenes no ejecutadas se cancelan a las 08:00 y las posiciones abiertas se cierran a las 15:45.

## Detalles

- **Criterios de entrada**:
  - Ruptura del máximo o mínimo del rango asiático tras la ventana de decisión.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Take profit o stop-loss basado en Fibonacci.
  - Todas las posiciones cerradas a las 15:45 NY.
- **Stops**: Multiplicadores de Fibonacci.
- **Valores predeterminados**:
  - `UseOptimizedFibo` = true
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Avanzado
  - Marco temporal: Cualquiera
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
