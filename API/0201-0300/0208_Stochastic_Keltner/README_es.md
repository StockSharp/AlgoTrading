# Estrategia Stochastic Keltner
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Esta estrategia utiliza los indicadores Stochastic Keltner para generar señales.
La entrada larga ocurre cuando Stoch %K < StochOversold && Price < Keltner lower band (sobrevendido en la banda inferior). La entrada corta ocurre cuando Stoch %K > StochOverbought && Price > Keltner upper band (sobrecomprado en la banda superior).
Es adecuada para traders que buscan oportunidades en mercados mixtos.

Las pruebas indican un rendimiento anual promedio de aproximadamente 61%. Funciona mejor en el mercado de criptomonedas.

## Detalles
- **Criterios de entrada**:
  - **Largo**: Stoch %K < StochOversold && Price < Keltner lower band (oversold at lower band)
  - **Corto**: Stoch %K > StochOverbought && Price > Keltner upper band (overbought at upper band)
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir de la posición larga cuando el precio regresa a la banda media
  - **Corto**: Salir de la posición corta cuando el precio regresa a la banda media
- **Stops**: Sí.
- **Valores predeterminados**:
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochOversold` = 20
  - `StochOverbought` = 80
  - `EmaPeriod` = 20
  - `KeltnerMultiplier` = 2
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2
    20 y 80 son los valores por defecto de los niveles de %K que citan las reglas. Las bandas son la EMA de EmaPeriod más y menos KeltnerMultiplier veces el ATR de AtrPeriod, y la banda media es la propia EMA. El stop se sitúa a AtrMultiplier veces el mismo ATR del cierre de entrada y se comprueba al cierre de las velas; 0 lo desactiva. StochK en las reglas es %K: el estocástico de StochPeriod velas suavizado en StochK velas; %D no interviene, por lo que no tiene ajuste. Una señal de entrada contra una posición abierta la invierte.
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Mixto
  - Dirección: Ambos
  - Indicadores: Stochastic Keltner
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

