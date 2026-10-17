# Estrategia Keltner con Filtro Estacional
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Opera rupturas del canal Keltner filtradas por rendimientos mensuales históricos. En velas completas de 5 minutos: `E = EMA(20)`, `A = ATR(14)`, `M = AtrMultiplier = 2`, `U = E + M × A`, `L = E − M × A`; `SeasonalThreshold = 0.5` puntos porcentuales. Espera indicadores formados y permiso para operar.

Compra con `S > SeasonalThreshold`, `Close > U`, `Position <= 0`; vende con `S < -SeasonalThreshold`, `Close < L`, `Position >= 0`. El volumen `Volume + abs(Position)` revierte toda posición contraria. Guarda un stop en `Close − M × A` para largos o `Close + M × A` para cortos, usando el cierre de señal.

Primero comprueba el stop en cada vela completa: cierra largos con `Close <= stop` o cortos con `Close >= stop`, borra el nivel y termina esa vela. En otro caso procesa entradas y después cierra largos bajo E o cortos sobre E. Es un stop manual comprobado al cierre y basado en el cierre de señal; no hay stop bursátil intravela, objetivo de beneficios ni trailing.

Requiere `SeasonalityHistoryFile`: CSV UTF-8 de aperturas y cierres mensuales con cabecera `SecurityId,Month,Open,Close`. `Month` usa `yyyy-MM`, los decimales un punto, los precios deben ser positivos y cada mes debe ser único para el instrumento. Ignora otros identificadores. La falta de datos, filas mal formadas o ausencia de observaciones coincidentes impiden iniciar.

En cada vela de señal, `S` es la media de `100 × (Close / Open − 1)` para ese mismo mes en años anteriores, dentro de `SeasonalityLookbackYears = 10`. Exige `MinimumSeasonalSamples = 2` observaciones. Excluye el mes actual y los futuros aunque aparezcan en el archivo. Sin historial suficiente no abre posiciones. Deben aportarse precios mensuales reales del instrumento; no hay una tabla fija de puntuaciones.
