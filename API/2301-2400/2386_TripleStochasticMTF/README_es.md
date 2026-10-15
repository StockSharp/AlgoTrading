# Estrategia TripleStochasticMTF
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia ejecuta tres Stochastic Oscillators en diferentes marcos temporales y opera cuando el marco temporal más pequeño cruza su línea de señal en la dirección confirmada por los marcos temporales superiores. Está diseñada para capturar reversiones de corto plazo dentro de un contexto de tendencia mayor.

El marco temporal primario (por defecto 30 minutos) y el secundario (por defecto 15 minutos) determinan el sesgo del mercado. El marco temporal de entrada (por defecto 5 minutos) espera un cruce de %K y %D opuesto a la barra anterior, señalando un retroceso. Las posiciones se cierran cuando cualquiera de los marcos temporales monitoreados señala un cambio de tendencia contra la operación activa.

## Detalles

- **Criterios de entrada**:
  - **Largo**: %K anterior > %D en el gráfico de 5 minutos, %K actual ≤ %D, y ambos marcos temporales superiores muestran %K > %D.
  - **Corto**: %K anterior < %D en el gráfico de 5 minutos, %K actual ≥ %D, y ambos marcos temporales superiores muestran %K < %D.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Cualquier marco temporal cambia a tendencia bajista (%K < %D).
  - **Corto**: Cualquier marco temporal cambia a tendencia alcista (%K > %D).
- **Stops**: No implementados por defecto.
- **Valores predeterminados**:
  - `Timeframe 1` = 30 minutos.
  - `Timeframe 2` = 15 minutos.
  - `Timeframe 3` = 5 minutos.
  - `%K Period` = 5.
  - `%D Period` = 3.
  - `Slowing` = 3.
- **Filtros**:
  - Categoría: Seguimiento de tendencia / Retroceso
  - Dirección: Ambos
  - Indicadores: Stochastic Oscillator
  - Stops: No
  - Complejidad: Medio
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Moderado
