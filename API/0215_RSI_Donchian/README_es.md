# RSI Donchian Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La estrategia RSI Donchian busca extremos de momentum que coincidan con rupturas del Canal Donchian. El índice de fuerza relativa mide las condiciones de sobrecompra y sobreventa mientras que el canal define los máximos y mínimos recientes del precio.

Las pruebas indican un rendimiento anual promedio de aproximadamente 82%. Funciona mejor en el mercado de acciones.

Aparece una señal de compra cuando el RSI cae por debajo de 30 y el precio rompe por encima de la banda superior Donchian. Una señal corta se forma cuando el RSI sube por encima de 70 y el precio cae a través de la banda inferior. Las salidas ocurren una vez que el precio regresa a la línea media Donchian, señalando un retorno al equilibrio.

Este método funciona bien para traders activos que prefieren operar contra movimientos de agotamiento pero aun así operan con niveles claros de ruptura. El stop-loss ayuda a limitar el riesgo si el momentum no revierte rápidamente.

## Detalles
- **Criterios de entrada**:
  - **Largo**: RSI < 30 && Close > Donchian High
  - **Corto**: RSI > 70 && Close < Donchian Low
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir cuando close < Donchian Middle
  - **Corto**: Salir cuando close > Donchian Middle
- **Stops**: Sí, stop-loss porcentual.
- **Valores predeterminados**:
  - `RsiPeriod` = 14
  - `DonchianPeriod` = 20
  - `StopLossPercent` = 2m
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

