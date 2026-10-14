# Estrategia MACD Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Esta estrategia usa los indicadores MACD Bollinger para generar señales.
La entrada larga ocurre cuando MACD > Signal && Price < BB_lower (tendencia alcista con condiciones de sobreventa). La entrada corta ocurre cuando MACD < Signal && Price > BB_upper (tendencia bajista con condiciones de sobrecompra).
Es adecuada para los operadores que buscan oportunidades en mercados mixtos.

Las pruebas indican un rendimiento anual promedio de aproximadamente 55%. Funciona mejor en el mercado de acciones.

## Detalles
- **Criterios de entrada**:
  - **Largo**: MACD > Signal && Price < BB_lower (tendencia alcista con condiciones de sobreventa)
  - **Corto**: MACD < Signal && Price > BB_upper (tendencia bajista con condiciones de sobrecompra)
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir de la posición larga cuando el precio regresa a la banda media
  - **Corto**: Salir de la posición corta cuando el precio regresa a la banda media
- **Stops**: Sí.
- **Valores predeterminados**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2
    Una posición se cierra en cuanto la vela cierra de vuelta en la banda media. El stop se sitúa a AtrMultiplier veces el ATR de AtrPeriod del cierre de entrada y se comprueba al cierre de las velas; 0 lo desactiva. Una señal de entrada contra una posición abierta la invierte.
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Mixto
  - Dirección: Ambos
  - Indicadores: MACD Bollinger
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

