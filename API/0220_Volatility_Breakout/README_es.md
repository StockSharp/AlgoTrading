# Estrategia de Ruptura de Volatilidad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Ruptura de Volatilidad busca movimientos direccionales fuertes cuando el precio escapa de su rango promedio. Midiendo la distancia desde una media móvil simple usando el ATR, el algoritmo define umbrales de ruptura que escalan con la volatilidad.

Las pruebas indican un retorno anual promedio de aproximadamente 97%. Funciona mejor en el mercado de criptomonedas.

Una orden de compra se activa cuando el cierre sube por encima de la SMA en más de `Multiplier` veces el ATR. Una señal de venta aparece cuando el cierre cae por debajo de la SMA la misma distancia. Las posiciones permanecen abiertas hasta que se produce una ruptura opuesta o se alcanza un stop de protección.

Esta técnica está orientada a los traders intradía que prosperan con los impulsos de momentum. Usar umbrales basados en ATR ayuda a filtrar el ruido para que solo los movimientos significativos generen operaciones.

## Detalles
- **Criterios de entrada**:
  - **Largo**: Close > SMA + Multiplier * ATR
  - **Corto**: Close < SMA - Multiplier * ATR
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir cuando se activa una ruptura opuesta o se alcanza el stop-loss
  - **Corto**: Salir cuando se activa una ruptura opuesta o se alcanza el stop-loss
- **Stops**: Sí, stop-loss a `Multiplier * ATR` desde la entrada.
- **Valores predeterminados**:
  - `Period` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: SMA, ATR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
