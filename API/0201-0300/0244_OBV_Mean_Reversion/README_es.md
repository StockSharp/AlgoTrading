# Estrategia de Reversión a la Media OBV
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El On Balance Volume (OBV) rastrea el flujo acumulativo de volumen para determinar si los compradores o vendedores son dominantes. Esta estrategia espera a que el OBV diverja marcadamente de su promedio y luego opera anticipando un retorno a niveles típicos.

Las pruebas indican un rendimiento anual promedio de aproximadamente 79%. Funciona mejor en el mercado de acciones.

Una señal de compra ocurre cuando el OBV cae por debajo de su media menos `Multiplier` veces la desviación estándar y el precio está por debajo de la media móvil. Una señal de venta se genera cuando el OBV sube por encima de la banda superior con el precio por encima de la media. Las posiciones se cierran cuando el OBV cruza de vuelta a través de su línea media.

El enfoque es útil para traders que consideran los flujos de volumen además de la acción del precio. Los stops se colocan a un porcentaje establecido para manejar situaciones donde el volumen continúa acelerando.

## Detalles
- **Criterios de entrada**:
  - **Largo**: OBV < Avg - Multiplier * StdDev && Close < MA
  - **Corto**: OBV > Avg + Multiplier * StdDev && Close > MA
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir cuando OBV > Avg
  - **Corto**: Salir cuando OBV < Avg
- **Stops**: Sí, stop-loss porcentual.
- **Valores predeterminados**:
  - `AveragePeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: OBV
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
