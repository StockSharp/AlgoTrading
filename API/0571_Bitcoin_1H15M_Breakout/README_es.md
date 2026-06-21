# Estrategia de Ruptura Bitcoin 1H-15M
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia sigue el máximo y el mínimo de la vela de 1 hora anterior y entra en operaciones cuando una vela de 15 minutos cierra fuera de ese rango. El riesgo se gestiona con un buffer de stop-loss fijo y un take-profit derivado de una relación riesgo-beneficio configurable.

## Detalles

- **Criterios de entrada**:
  - Cierre de 15 minutos por encima del máximo de la hora anterior → entrada larga.
  - Cierre de 15 minutos por debajo del mínimo de la hora anterior → entrada corta.
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Stop loss a distancia de buffer fija.
  - Take profit a buffer × relación riesgo-beneficio.
- **Stops**: Stop loss y take profit mediante módulo de protección.
- **Valores predeterminados**:
  - Marco temporal inferior = 15 minutos.
  - Marco temporal superior = 1 hora.
  - Buffer de stop loss = 50.
  - Relación riesgo-beneficio = 2.0.
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: SL & TP
  - Complejidad: Bajo
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
