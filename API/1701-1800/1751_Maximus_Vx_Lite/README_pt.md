# Estratégia Maximus vX Lite
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia tenta negociar rompimentos de zonas de consolidação de curto prazo. Ela procura faixas de preço compactas no gráfico de 15 minutos e abre negociações quando o preço se afasta dessas faixas por uma distância especificada.

## Lógica da estratégia

1. Para cada candle de 15 minutos finalizado, são calculados a máxima mais alta e a mínima mais baixa dos últimos 40 candles.
2. Se a distância entre esses extremos for inferior ao parâmetro **Range**, uma zona de consolidação é assumida.
3. Após o período **Delay Open** passar sem novas negociações, um rompimento acima do limite superior mais **Distance** pontos aciona uma posição comprada, enquanto um rompimento abaixo do limite inferior menos **Distance** pontos aciona uma posição vendida.
4. Um **Stop Loss** fixo e um trailing stop de **Trail** pontos são aplicados assim que uma posição é aberta.
5. Os limites de consolidação são redefinidos após decorridas as horas de **Period**.

## Parâmetros

- `DelayOpen` – Horas de espera antes de abrir uma nova negociação.
- `Distance` – Distância de rompimento a partir do limite de consolidação em pontos.
- `Period` – Horas após as quais os níveis de consolidação são recalculados.
- `Range` – Tamanho máximo da zona de consolidação em pontos.
- `StopLoss` – Stop loss inicial em pontos.
- `Trail` – Distância do trailing stop em pontos.

## Notas

A estratégia usa apenas a API de alto nível: os candles são recebidos via `SubscribeCandles`, e os valores dos indicadores são vinculados usando `Bind`. As ordens são enviadas com os métodos `BuyMarket` e `SellMarket`. Os comentários no código-fonte estão escritos em inglês.
