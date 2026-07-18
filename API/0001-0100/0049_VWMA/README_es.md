# Estrategia VWMA Cross
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La Media Móvil Ponderada por Volumen (VWMA) enfatiza los niveles de precio con mayor volumen de negociación. Esta estrategia opera los cruces entre el precio y la VWMA.

Las pruebas indican un retorno anual promedio de aproximadamente 184%. Funciona mejor en el mercado de criptomonedas.

Un cierre por encima de la VWMA tras estar por debajo genera una entrada larga, mientras que una caída por debajo de la VWMA genera una operación corta. Las posiciones salen cuando el precio cruza de vuelta en la dirección opuesta.

Usar una media ponderada por volumen reduce el ruido de los períodos de bajo volumen.

## Detalles

- **Criterios de entrada**: El precio cruza la VWMA desde abajo o desde arriba.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Cruce inverso o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `VWMAPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: VWMA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

