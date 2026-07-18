# Estratégia de Notícias Simples
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia coloca ordens stop pendentes em torno de um horário de notícia especificado para capturar movimentos bruscos causados por divulgações de notícias.

## Como funciona

- Começando cinco minutos antes de `NewsTime`, a estratégia envia pares de ordens buy stop e sell stop.
- O primeiro par é colocado a `Distance` pips do preço ask e bid atuais.
- Os pares adicionais são deslocados `Delta` pips em relação aos anteriores, totalizando `Deals` pares.
- Dez minutos após a divulgação da notícia, a estratégia cancela todas as ordens que não foram ativadas.
- Quando uma posição é aberta, a estratégia monitora os níveis de stop-loss, take-profit e trailing stop. Se qualquer nível for atingido, a posição é fechada.

## Parâmetros

- `NewsTime` – momento da divulgação da notícia.
- `Deals` – número de pares de ordens buy/sell stop.
- `Delta` – espaçamento entre ordens em pips.
- `Distance` – distância do preço atual para o primeiro par em pips.
- `StopLoss` – stop-loss inicial em pips.
- `Trail` – trailing stop em pips.
- `TakeProfit` – take-profit em pips.
- `Volume` – volume da ordem.

## Notas

A estratégia não depende de indicadores e funciona exclusivamente com dados de nível 1. Destina-se a fins de demonstração e pode exigir ajustes para o trading real.
