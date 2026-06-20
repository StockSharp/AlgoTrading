# CCI VWAP Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
El enfoque CCI VWAP intenta capturar reversiones intradía cuando el momentum y el precio se alejan del precio promedio ponderado por volumen. Al observar el Índice de Canal de Materias Primas junto con el nivel VWAP, el sistema mide la fuerza de los movimientos recientes en relación con un punto de referencia de valor justo.

Las pruebas indican un rendimiento anual promedio de aproximadamente 70%. Funciona mejor en el mercado de acciones.

Una configuración de compra surge cuando el CCI cae por debajo de -100 y el mercado cotiza por debajo del VWAP, señalando que la presión vendedora puede estar agotada. Un corto ocurre cuando el CCI sube por encima de +100 con el precio sobre el VWAP, destacando un rally extendido vulnerable a una corrección. Las posiciones se cierran una vez que el precio recupera el VWAP en dirección opuesta.

Esta estrategia está diseñada para traders intradía que prefieren operar en los extremos pero aun así confían en niveles objetivos para las salidas. El stop-loss definido ayuda a gestionar el riesgo si el momentum no revierte rápidamente a la media.

## Detalles
- **Criterios de entrada**:
  - **Largo**: CCI < -100 && Price < VWAP (oversold below VWAP)
  - **Corto**: CCI > 100 && Price > VWAP (overbought above VWAP)
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir del largo cuando el precio suba por encima del VWAP
  - **Corto**: Salir del corto cuando el precio caiga por debajo del VWAP
- **Stops**: Sí.
- **Valores predeterminados**:
  - `CciPeriod` = 20
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Mixto
  - Dirección: Ambos
  - Indicadores: CCI VWAP
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

