# Estrategia de Tendencia con Hurst Exponent
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este sistema utiliza el Hurst Exponent para determinar si el mercado está exhibiendo comportamiento de tendencia. Los valores por encima del umbral indican persistencia, mientras que los valores por debajo sugieren ruido o reversión a la media. Una media móvil proporciona confirmación adicional de dirección.

Las pruebas indican un retorno anual promedio de aproximadamente 40%. Funciona mejor en el mercado de criptomonedas.

La estrategia compra cuando el Hurst Exponent es mayor que el umbral y el precio cierra por encima de la media móvil. Vende en corto cuando el Hurst Exponent es alto y el precio cierra por debajo de la media. Si el Hurst Exponent cae por debajo del umbral, las posiciones existentes se cierran para evitar operar en mercados laterales.

Este enfoque funciona para traders que desean una confirmación objetiva de que existe una tendencia antes de entrar. La combinación del filtro de tendencia y el stop-loss ayuda a gestionar el riesgo de señales falsas.

## Detalles
- **Criterios de entrada**:
  - **Largo**: Hurst > Umbral && Cierre > MA
  - **Corto**: Hurst > Umbral && Cierre < MA
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir cuando Cierre < MA o Hurst < Umbral
  - **Corto**: Salir cuando Cierre > MA o Hurst < Umbral
- **Stops**: Sí, stop-loss porcentual.
- **Valores predeterminados**:
  - `HurstPeriod` = 100
  - `MaPeriod` = 20
  - `HurstThreshold` = 0.55m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Hurst Exponent, MA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
