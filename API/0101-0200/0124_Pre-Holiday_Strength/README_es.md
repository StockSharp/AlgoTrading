# Estrategia de Fortaleza Pre-Festiva
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La Fortaleza Pre-Festiva se refiere a la tendencia alcista justo antes de los principales festivos del mercado, cuando el volumen es más bajo y el sentimiento optimista.
Los operadores suelen posicionarse antes del cierre, empujando los precios al alza en la última sesión o dos.

Las pruebas indican un retorno anual promedio de aproximadamente el 109%. Funciona mejor en el mercado cripto.

La estrategia toma posiciones largas el día antes del festivo y sale en la siguiente sesión o al cierre, capturando ese sesgo a corto plazo.

Se usa un stop ajustado en caso de que el alza esperada no se produzca.

## Detalles

- **Criterios de entrada**: activadores de efecto de calendario
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `Holidays` = 2024 NYSE holidays
  - `StopLossPercent` = 2
    Los días son días UTC, ya que el mercado opera las 24 horas; las entradas ocurren al cierre de la primera vela del día. Holidays es una lista de fechas yyyy-MM-dd separadas por comas, por defecto los festivos de la NYSE de 2024 (Viernes Santo es 2024-03-29). El mercado opera todos los días, así que la compra se abre el día natural anterior a un festivo y se cierra en la última vela de ese día.
- **Filtros**:
  - Categoría: Estacionalidad
  - Dirección: Ambos
  - Indicadores: Estacionalidad
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

