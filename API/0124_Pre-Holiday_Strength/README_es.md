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
  - `StopLoss` = 2%
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

