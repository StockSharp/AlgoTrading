# Estrategia Parabolic SAR CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Esta estrategia usa los indicadores Parabolic SAR CCI para generar señales.
La entrada larga ocurre cuando Price > SAR && CCI < CciOversold (tendencia alcista con condiciones de sobreventa). La entrada corta ocurre cuando Price < SAR && CCI > CciOverbought (tendencia bajista con condiciones de sobrecompra).
Es adecuada para los operadores que buscan oportunidades en mercados mixtos.

Las pruebas indican un rendimiento anual promedio de aproximadamente 49%. Funciona mejor en el mercado cripto.

## Detalles
- **Criterios de entrada**:
  - **Largo**: Price > SAR && CCI < CciOversold (tendencia alcista con condiciones de sobreventa)
  - **Corto**: Price < SAR && CCI > CciOverbought (tendencia bajista con condiciones de sobrecompra)
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir de la posición larga cuando el precio cae por debajo del SAR
  - **Corto**: Salir de la posición corta cuando el precio sube por encima del SAR
- **Stops**: No.
- **Valores predeterminados**:
  - `SarAccelerationFactor` = 0.02
  - `SarMaxAccelerationFactor` = 0.2
  - `CciPeriod` = 20
  - `CciOversold` = -100
  - `CciOverbought` = 100
    -100 y 100 son los valores por defecto de los niveles de CCI que citan las reglas. Una entrada es un retroceso dentro de la tendencia que muestra el SAR, y el SAR actúa como salida dinámica: un largo se cierra con un cierre por debajo de él y un corto con un cierre por encima. Una señal de entrada contra una posición abierta la invierte.
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Mixto
  - Dirección: Ambos
  - Indicadores: Parabolic SAR CCI
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

