# Aver4 Stoch Post ZigZag
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Combina cuatro osciladores Stoch en múltiples horizontes temporales y un detector simple de pivotes ZigZag. El Stoch promedio guía los niveles de sobrecompra/sobreventa mientras que el ZigZag confirma máximos y mínimos de oscilación. Las compras ocurren cuando el Stoch promediado cae por debajo del nivel de sobreventa y se forma un nuevo mínimo ZigZag. Las ventas ocurren cuando el Stoch promediado sube por encima del nivel de sobrecompra y se forma un nuevo máximo ZigZag. Las posiciones opuestas existentes se cierran al revertirse la señal.

## Detalles
- **Criterios de entrada**: Stoch promediado cruzando zonas de sobreventa/sobrecompra con pivote ZigZag coincidente.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta.
- **Stops**: StartProtection 2%/2% (predeterminado).
- **Valores predeterminados**:
  - `ShortLength` = 26
  - `MidLength1` = 72
  - `MidLength2` = 144
  - `LongLength` = 288
  - `ZigZagDepth` = 14
  - `Oversold` = 5
  - `Overbought` = 95
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: Stochastic, ZigZag
  - Stops: Sí
  - Complejidad: Avanzado
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
