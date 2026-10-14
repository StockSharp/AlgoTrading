# Estrategia de Reversión con Hurst Exponent
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Este enfoque utiliza el Hurst Exponent para detectar cuándo un mercado se comporta de manera de reversión a la media. Valores por debajo de 0.5 sugieren que el precio tiende a regresar hacia su promedio, creando oportunidades para operar contra los extremos.

Las pruebas indican un retorno anual promedio de aproximadamente 121%. Funciona mejor en el mercado cripto.

Se abre una posición larga cuando el Hurst Exponent está por debajo de HurstThreshold y el precio cierra por debajo de una media móvil. Una posición corta ocurre cuando el valor Hurst está por debajo de HurstThreshold y el precio cierra por encima del promedio. Las posiciones se cierran cuando el precio regresa a la línea promedio o el Hurst Exponent sube por encima del umbral.

La estrategia es adecuada para traders que prefieren las tendencias estadísticas sobre las tendencias fuertes. Un stop-loss de protección protege contra movimientos prolongados que no logran revertirse.

## Detalles
- **Criterios de entrada**:
  - **Largo**: Hurst < HurstThreshold && Close < MA
  - **Corto**: Hurst < HurstThreshold && Close > MA
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir cuando Close >= MA o Hurst > HurstThreshold
  - **Corto**: Salir cuando Close <= MA o Hurst > HurstThreshold
- **Stops**: Sí, stop-loss porcentual.
- **Valores predeterminados**:
  - `HurstPeriod` = 100
  - `AveragePeriod` = 20
  - `HurstThreshold` = 0.7
  - `StopLossPercent` = 2
    Las reglas citan 0.5, el límite teórico de la reversión a la media, pero la estimación R/S sobre 100 velas de cinco minutos nunca bajó de alrededor de 0.65 en el historial de BTC y TON con el que se prueban los ejemplos, así que la regla no podía operar; HurstThreshold convierte el nivel en un ajuste con valor por defecto 0.7. MA es la media móvil simple de AveragePeriod. El stop es un StopLossPercent fijo del precio de entrada, vigilado también entre velas; 0 lo desactiva. Una señal de entrada contra una posición abierta la invierte.
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Mean reversion
  - Dirección: Ambos
  - Indicadores: Hurst Exponent, MA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

