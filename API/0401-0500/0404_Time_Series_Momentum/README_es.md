# Estrategia de Momentum de Series Temporales
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este enfoque toma posiciones largas o cortas en cada activo según sus propios retornos pasados. Si el retorno acumulado es positivo, el modelo compra; si es negativo, vende, formando una cartera de seguimiento de tendencia diversificada.

Las señales se evalúan mensualmente con periodos de retrospección de un año y las posiciones se ponderan de forma equitativa entre los activos.

## Detalles

- **Datos**: Retornos totales mensuales de cada activo.
- **Entrada**: Largo cuando el retorno a 12 meses > 0; corto cuando < 0.
- **Salida**: Invertir cuando la señal cambia de signo.
- **Instrumentos**: Amplio conjunto de futuros o ETF.
- **Riesgo**: Escalado por volatilidad y diversificación.

