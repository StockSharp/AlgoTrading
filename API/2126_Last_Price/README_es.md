# Estrategia de Último Precio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia coloca órdenes limitadas en la mejor oferta o demanda cuando el último precio de operación se aleja por un intervalo definido por el usuario. Escucha las actualizaciones del libro de órdenes Level1 y las operaciones ejecutadas para decidir las entradas.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Último precio ≥ mejor ask + intervalo.
  - **Corto**: Último precio ≤ mejor bid - intervalo.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**:
  - Señal opuesta o fuera de las sesiones de trading permitidas.
- **Stops**: Solo stop loss.
- **Valores predeterminados**:
  - `Interval` = 400
  - `Min Volume` = 1
  - `Max Volume` = 900000
  - `Spread` = 200
  - `Volume` = 1
  - `Stop Loss` = 400
- **Sesiones de trading**:
  - 10:05:40 – 13:54:30
  - 14:08:30 – 15:44:30
  - 16:05:30 – 18:39:30
  - 19:15:10 – 23:44:30
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
