# Estratégia de Operação na Hora de Notícias
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **News Hour Trade** coloca ordens stop de compra e venda pendentes em torno de eventos de notícias de alto impacto programados. As ordens são deslocadas do preço atual por um número fixo de passos e incluem gestão de stop-loss, take-profit e trailing stop opcional.

## Ideia

1. Na hora e minuto de início configurados, a estratégia se prepara para um próximo comunicado de notícias.
2. Uma ordem buy stop e uma sell stop são colocadas `PriceGap` passos acima e abaixo do preço atual.
3. Quando uma ordem é acionada, a ordem pendente oposta é cancelada automaticamente.
4. A posição aberta é protegida com níveis fixos de stop-loss e take-profit. Se `TrailStop` estiver habilitado, o nível de stop segue o preço quando ele se move a favor da posição.
5. Apenas uma operação por dia é permitida.

## Parâmetros

- **StartHour / StartMinute** – hora de início do trading.
- **DelaySeconds** – pausa antes de colocar as ordens (atualmente informativo).
- **Volume** – tamanho da ordem em lotes.
- **StopLoss** – distância ao stop-loss em passos de preço.
- **TakeProfit** – distância ao take-profit em passos.
- **PriceGap** – deslocamento do preço atual para ordens pendentes.
- **Expiration** – tempo de vida da ordem pendente em segundos (0 significa sem expiração).
- **TrailStop** – habilitar trailing stop.
- **TrailingStop** – distância do preço atual para o trailing stop.
- **TrailingGap** – gap mínimo antes de atualizar o trailing stop.
- **BuyTrade / SellTrade** – habilitar ordens de compra ou venda.
- **CandleType** – período utilizado para rastreamento de tempo.

## Notas

A estratégia é destinada ao período M5, mas pode ser aplicada a qualquer instrumento com spreads baixos. Use com cautela durante eventos de notícias importantes.
