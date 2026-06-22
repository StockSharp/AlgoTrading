# Estratégia de Velas com Filtro Kalman
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia aplica o Filtro Kalman aos preços de abertura e fechamento de cada vela. As velas suavizadas resultantes são classificadas como de alta ou de baixa dependendo de se o fechamento suavizado está acima ou abaixo da abertura suavizada. As posições são abertas quando a cor da vela muda:

- **Alta (rosa)** &rarr; abre uma posição comprada e fecha qualquer posição vendida.
- **Baixa (azul)** &rarr; abre uma posição vendida e fecha qualquer posição comprada.

## Parâmetros

- `Process Noise` &ndash; fator de suavização para o Filtro Kalman.
- `Candle Type` &ndash; período das velas utilizadas na estratégia.

## Como Funciona

1. Para cada vela finalizada, os preços de abertura e fechamento são suavizados individualmente usando Filtros Kalman separados.
2. Um sinal de alta é gerado quando o fechamento suavizado supera a abertura suavizada. Um sinal de baixa ocorre quando o fechamento suavizado está abaixo da abertura suavizada.
3. A estratégia entra em uma posição comprada com um sinal de alta e em uma posição vendida com um sinal de baixa. As posições opostas são fechadas automaticamente.

A estratégia é concebida como exemplo de combinação de múltiplos Filtros Kalman para formar um sistema simples de seguidor de tendência.
