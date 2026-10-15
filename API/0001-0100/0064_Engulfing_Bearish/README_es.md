# Estrategia de Patrón de Envolvente Bajista
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Este patrón pretende capturar el inicio de un movimiento bajista tras un repunte. Una envolvente bajista ocurre cuando una vela roja engulle por completo el cuerpo alcista anterior. Contar algunas barras consecutivas al alza antes del patrón asegura que el mercado estaba subiendo previamente.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 79%. Funciona mejor en el mercado de acciones.

El algoritmo almacena cada vela en secuencia. Si la nueva barra cierra más bajo de lo que abre y su cuerpo engulle la barra alcista anterior, se ejecuta una venta en corto. El stop-loss se posiciona por encima del máximo del patrón para limitar la exposición.

Las posiciones se gestionan típicamente con el stop protector, aunque el operador puede salir manualmente si las condiciones cambian. Requerir una tendencia alcista ayuda a evitar señales falsas durante mercados irregulares.

## Detalles

- **Criterios de entrada**: La vela bajista engulle la barra alcista anterior, con tendencia alcista opcional presente.
- **Largo/Corto**: Solo cortos.
- **Criterios de salida**: Stop-loss o discrecional.
- **Stops**: Sí, por encima del máximo del patrón.
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 1
  - `RequireUptrend` = true
  - `UptrendBars` = 3
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Corto
  - Indicadores: Candlestick
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

