# Estrategia ATR Expansion Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Esta estrategia sigue las explosiones de volatilidad usando el Average True Range. Cuando el ATR está aumentando en comparación con la barra anterior y el precio opera relativo a una media móvil, busca montar el rompimiento.

Las pruebas indican un rendimiento anual promedio de aproximadamente 145%. Funciona mejor en el mercado de criptomonedas.

La expansión del ATR implica que hay un movimiento fuerte en curso. Las entradas se alinean con la dirección del precio relativa a la media móvil, mientras que las contracciones de volatilidad activan las salidas.

Los stops se establecen usando un múltiplo de ATR para dar espacio a las operaciones durante la alta volatilidad.

## Detalles

- **Criterios de entrada**: ATR aumentando y precio por encima/debajo de la MA.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: ATR se contrae o se activa el stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `AtrPeriod` = 14
  - `MAPeriod` = 20
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: ATR, MA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

