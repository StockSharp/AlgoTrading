# Estratégia SuperATR de Lucro em 7 Etapas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Combina um filtro de tendência ATR adaptativo com um sistema de tomada de lucro de sete estágios. O ATR normalizado por momentum define a força da tendência, enquanto os egressos ocorrem quando a média móvel curta se alinha com a direção da tendência confirmada.

- **Comprado**: Força da tendência acima do limiar, preço acima da MA curta e MA curta acima da MA longa.
- **Vendido**: Força da tendência abaixo do limiar negativo, preço abaixo da MA curta e MA curta abaixo da MA longa.
- **Indicadores**: Momentum, Standard Deviation, SMA, ATR.
- **Tomada de lucro**: Quatro níveis baseados em ATR e três níveis de percentagem fixa, cada um fechando uma porção da posição quando ativado.

