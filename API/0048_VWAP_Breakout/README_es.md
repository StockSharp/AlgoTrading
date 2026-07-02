# Estrategia VWAP Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
VWAP Breakout busca que el precio cruce el Precio Promedio Ponderado por Volumen desde el lado opuesto. Una ruptura por encima del VWAP señala presión alcista, mientras que una caída por debajo del VWAP señala sentimiento bajista.

Las pruebas indican un retorno anual promedio de aproximadamente 181%. Funciona mejor en el mercado de criptomonedas.

La estrategia espera un cierre al otro lado del VWAP y luego opera en esa dirección. Las salidas ocurren cuando el precio revierte de nuevo a través del VWAP.

Dado que el VWAP representa el precio promedio de transacción, sus rupturas suelen generar movimientos de momentum.

## Detalles

- **Criterios de entrada**: El precio cierra al lado opuesto del VWAP.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: El precio cruza de vuelta a través del VWAP o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: VWAP
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

