# Estratégia EMA Grid Martingale com Cooldown
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Implementa um sistema de grid somente comprado baseado em EMA com dimensionamento martingala opcional e cooldown entre os grids. Um novo grid começa quando ambas as EMAs rápidas cruzam acima das suas contrapartes lentas. Compras adicionais são realizadas em intervalos fixos de pips, e a posição é fechada ao preço médio ponderado mais um buffer.
