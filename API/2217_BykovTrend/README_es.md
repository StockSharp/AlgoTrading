# Estrategia BykovTrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia reproduce el sistema clásico de MetaTrader "Bykov Trend" utilizando la API de alto nivel de StockSharp. El indicador original combina el oscilador Williams %R con un mecanismo simple de detección de tendencia. Cuando la tendencia cambia de bajista a alcista, se abre una posición larga. Cuando la tendencia cambia de alcista a bajista, se abre una posición corta.

El sistema opera un único instrumento en un marco temporal seleccionado. Solo se mantiene una posición a la vez; las señales opuestas invierten la posición.

## Detalles

- **Criterios de entrada**  
  - **Largo**: Williams %R sube por encima de `-K` después de estar por debajo de `-100 + K` (`K = 33 - Risk`).  
  - **Corto**: Williams %R cae por debajo de `-100 + K` después de estar por encima de `-K`.
- **Largo/Corto**: Ambas direcciones.  
- **Criterios de salida**  
  - La señal opuesta cierra la posición actual y abre una nueva en la dirección contraria.  
- **Stops**: Ninguno.  
- **Valores predeterminados**  
  - `Risk` = 3 (`K = 30`).  
  - `SSP` = 9 (retroceso de Williams %R).  
  - `CandleType` = velas de 1 hora.  
- **Filtros**  
  - Categoría: Seguimiento de tendencia  
  - Dirección: Ambos  
  - Indicadores: Único (Williams %R)  
  - Stops: No  
  - Complejidad: Simple  
  - Marco temporal: Flexible  
  - Estacionalidad: No  
  - Redes neuronales: No  
  - Divergencia: No  
  - Nivel de riesgo: Medio
