# Estratégia de Cruzamento de EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera o cruzamento de duas médias móveis exponenciais (EMA).
Uma posição comprada é aberta quando a EMA rápida cruza acima da EMA lenta, enquanto uma posição vendida é aberta quando a EMA rápida cruza abaixo da EMA lenta.
O parâmetro **Reverse** troca os papéis das EMA, invertendo efetivamente os sinais de entrada.

Cada posição é protegida por níveis fixos de **Take Profit** e **Stop Loss**.
Um **Trailing Stop** opcional segue o preço quando ele se move na direção favorável, assegurando lucros.

A estratégia processa apenas velas terminadas e usa ligação de API de alto nível para indicadores e assinaturas de velas.

## Parâmetros
- Tipo de vela
- Comprimento da EMA rápida
- Comprimento da EMA lenta
- Take profit
- Stop loss
- Trailing stop
- Reverse
