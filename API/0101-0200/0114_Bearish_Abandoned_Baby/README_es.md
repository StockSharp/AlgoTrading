# Estrategia Bebé Abandonado Bajista
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
El Bebé Abandonado Bajista refleja la versión alcista pero señala un posible techo.
Presenta un doji con gap alcista seguido de un gap bajista, dejando la vela del medio aislada por encima del rango anterior.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 79%. Funciona mejor en el mercado de acciones.

La estrategia vende en corto cuando la tercera vela abre con gap por debajo del doji, con el objetivo de beneficiarse del cambio abrupto en el sentimiento.

El riesgo está limitado con un stop justo por encima del máximo del doji en caso de que el precio se recupere.

## Detalles

- **Criterios de entrada**: coincidencia de patrón
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `DojiBodyPercent` = 10
  - `StopLossPercent` = 2
    El mercado opera las 24 horas, así que los rangos de las velas casi nunca dejan huecos (el mes de muestra tiene como mucho un patrón bajista así); los huecos se miden entre los cuerpos de las velas. El cuerpo del doji es como mucho DojiBodyPercent de su rango. La estrategia solo vende, al cierre de la tercera vela; el stop se coloca StopLossPercent por encima del máximo del doji y la posición se cierra cuando una vela cierra por encima.
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Ambos
  - Indicadores: Candlestick
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

