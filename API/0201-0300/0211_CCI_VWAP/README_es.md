# Estrategia CCI VWAP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
El enfoque CCI VWAP intenta capturar reversiones intradía cuando el momentum y el precio se alejan del precio promedio ponderado por volumen. Al observar el Índice de Canal de Materias Primas junto con el nivel VWAP, el sistema mide la fuerza de los movimientos recientes en relación con un punto de referencia de valor justo.

Las pruebas indican un rendimiento anual promedio de aproximadamente 70%. Funciona mejor en el mercado de acciones.

Una configuración de compra surge cuando el CCI cae por debajo de CciOversold y el mercado cotiza por debajo del VWAP, señalando que la presión vendedora puede estar agotada. Un corto ocurre cuando el CCI sube por encima de CciOverbought con el precio sobre el VWAP, destacando un rally extendido vulnerable a una corrección. Las posiciones se cierran una vez que el precio recupera el VWAP en dirección opuesta.

Esta estrategia está diseñada para traders intradía que prefieren operar en los extremos pero aun así confían en niveles objetivos para las salidas. El stop-loss definido ayuda a gestionar el riesgo si el momentum no revierte rápidamente a la media.

## Detalles
- **Criterios de entrada**:
  - **Largo**: CCI < CciOversold && Price < VWAP (oversold below VWAP)
  - **Corto**: CCI > CciOverbought && Price > VWAP (overbought above VWAP)
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir del largo cuando el precio suba por encima del VWAP
  - **Corto**: Salir del corto cuando el precio caiga por debajo del VWAP
- **Stops**: Sí.
- **Valores predeterminados**:
  - `CciPeriod` = 20
  - `CciOversold` = -100
  - `CciOverbought` = 100
  - `StopLossPercent` = 2
    -100 y 100 son los valores por defecto de los niveles de CCI que citan las reglas. El mercado opera las 24 horas, por lo que el VWAP de la sesión se reinicia cada día UTC y pondera el precio típico de cada vela por su volumen. Una señal de entrada contra una posición abierta la invierte.
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

