# Estrategia Forex Fraus 4 For M1s
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Conversión de la estrategia MQL4 #13643. El asesor experto original entra en operaciones cuando el indicador Williams %R toca niveles extremos y luego cruza de vuelta. Esta versión en C# utiliza la API de alto nivel de StockSharp.

La estrategia funciona en velas de 1 minuto y reacciona a dos niveles clave:
- Una señal larga se genera después de que Williams %R sube por encima de -99.9 habiendo estado por debajo.
- Una señal corta aparece cuando Williams %R cae por debajo de -0.1 habiendo estado por encima.

Las posiciones se cierran por stops fijos, objetivos o trailing stop. Un filtro de tiempo puede restringir las operaciones a una ventana intradiaria específica.

## Detalles

- **Criterios de entrada**  
  - Largo: `WilliamsR` cruza hacia arriba `BuyThreshold` (-99.9) después de estar por debajo.  
  - Corto: `WilliamsR` cruza hacia abajo `SellThreshold` (-0.1) después de estar por encima.
- **Largo/Corto**: Ambos
- **Criterios de salida**  
  - El precio alcanza el stop-loss (`StopLoss`) o el take-profit (`TakeProfit`)  
  - Trailing stop (`TrailingStop`) activado cuando está habilitado
- **Stops**: Basados en pasos
- **Valores predeterminados**  
  - `WprPeriod` = 360  
  - `BuyThreshold` = -99.9  
  - `SellThreshold` = -0.1  
  - `StopLoss` = 0  
  - `TakeProfit` = 0  
  - `UseProfitTrailing` = true  
  - `TrailingStop` = 30  
  - `TrailingStep` = 1  
  - `UseTimeFilter` = false  
  - `StartHour` = 7  
  - `StopHour` = 17  
  - `Volume` = 0.01  
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**  
  - Categoría: Reversión de tendencia  
  - Dirección: Ambos  
  - Indicadores: Williams %R  
  - Stops: Sí  
  - Complejidad: Básico  
  - Marco temporal: Intradía (M1)  
  - Estacionalidad: No  
  - Redes neuronales: No  
  - Divergencia: No  
  - Nivel de riesgo: Medio
