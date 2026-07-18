# Estrategia de Ciclo de Tendencia Color Schaff JCCX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una conversión en C# del experto MQL5 `Exp_ColorSchaffJCCXTrendCycle`.
Emplea el oscilador **Schaff Trend Cycle (STC)** construido sobre el algoritmo JCCX.

## Lógica de trading

* Calcular el Schaff Trend Cycle en cada vela terminada.
* Cuando el oscilador cae por debajo del `High Level` tras haber estado por encima, se abre una posición larga y se cierran las posiciones cortas.
* Cuando el oscilador sube por encima del `Low Level` tras haber estado por debajo, se abre una posición corta y se cierran las posiciones largas.

## Parámetros

| Nombre | Descripción |
|------|-------------|
| Fast JCCX | Período JCCX rápido utilizado en el indicador. |
| Slow JCCX | Período JCCX lento utilizado en el indicador. |
| Smoothing | Factor de suavizado JJMA para JCCX. |
| Phase | Valor de fase JJMA. |
| Cycle | Longitud del ciclo para el cálculo del Schaff Trend. |
| High Level | Nivel de disparo superior del oscilador. |
| Low Level | Nivel de disparo inferior del oscilador. |
| Open Long | Permitir apertura de posiciones largas. |
| Open Short | Permitir apertura de posiciones cortas. |
| Close Long | Permitir cierre de posiciones largas existentes. |
| Close Short | Permitir cierre de posiciones cortas existentes. |

## Notas

La estrategia utiliza la API de alto nivel de StockSharp y se suscribe a datos de velas. Reacciona únicamente a velas **terminadas**. La gestión del dinero y el control de riesgos se mantienen simples con fines demostrativos.
