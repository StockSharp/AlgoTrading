# Estrategia CoeffofLine True
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia porta el experto MQL5 `Exp_CoeffofLine_true.mq5` al framework StockSharp. Rastrea la **Pendiente de Regresión Lineal** de los precios medianos y reacciona a los cruces de cero.

Una posición larga se abre cuando la pendiente se vuelve positiva después de ser negativa. Una posición corta se abre cuando la pendiente se vuelve negativa después de ser positiva. Las posiciones existentes se cierran ante señales opuestas. Solo se procesan las velas completadas.

## Parámetros

- **Candle Type** – marco temporal para la serie de velas.
- **Slope Period** – longitud de la regresión lineal utilizada para calcular la pendiente.
- **Signal Bar** – índice de barra histórica utilizado para la evaluación de señales.
- **Buy Open / Sell Open** – permisos para abrir posiciones largas o cortas.
- **Buy Close / Sell Close** – permisos para cerrar posiciones largas o cortas.

La estrategia se suscribe a las velas, vincula el indicador a través de la API de alto nivel y opera sin solicitudes manuales de valores del indicador.
