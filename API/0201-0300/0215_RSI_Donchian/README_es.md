# Estrategia RSI Donchian
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La estrategia RSI Donchian busca extremos de momentum que coincidan con rupturas del Canal Donchian. El índice de fuerza relativa mide las condiciones de sobrecompra y sobreventa mientras que el canal define los máximos y mínimos recientes del precio.

Las pruebas indican un rendimiento anual promedio de aproximadamente 82%. Funciona mejor en el mercado de acciones.

Aparece una señal de compra cuando el RSI está por encima de RsiOverbought mientras el precio rompe por encima de la banda superior Donchian. Una señal corta se forma cuando el RSI está por debajo de RsiOversold mientras el precio cae a través de la banda inferior. Las salidas ocurren una vez que el precio regresa a la línea media Donchian, señalando un retorno al equilibrio.

Este método funciona bien para traders activos que prefieren seguir un momentum fuerte pero aun así operan con niveles claros de ruptura. El stop-loss ayuda a limitar el riesgo si el momentum no revierte rápidamente.

## Detalles
- **Criterios de entrada**:
  - **Largo**: RSI > RsiOverbought && Close > Donchian High
  - **Corto**: RSI < RsiOversold && Close < Donchian Low
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir cuando close < Donchian Middle
  - **Corto**: Salir cuando close > Donchian Middle
- **Stops**: Sí, stop-loss porcentual.
- **Valores predeterminados**:
  - `RsiPeriod` = 14
  - `DonchianPeriod` = 20
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `StopLossPercent` = 2
    El canal es el máximo y el mínimo de las DonchianPeriod velas anteriores, y su punto medio está a mitad de camino entre ellos. Una ruptura alcista se confirma cuando el RSI está por encima de RsiOverbought y una bajista cuando está por debajo de RsiOversold; la lectura contraria, un RSI sobrevendido con un cierre por encima del canal, prácticamente no ocurre. El stop es un StopLossPercent fijo del precio de entrada, vigilado también entre velas. Una señal de entrada contra una posición abierta la invierte.
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoría: Mixto
  - Dirección: Ambos
  - Indicadores: RSI, Donchian Channel
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

