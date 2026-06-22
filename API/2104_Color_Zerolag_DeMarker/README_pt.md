# Estratégia Color Zerolag DeMarker
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia converte o especialista MQL5 original `Exp_ColorZerolagDeMarker` para o framework StockSharp. Ela usa uma combinação personalizada de vários indicadores **DeMarker** para construir linhas de tendência rápidas e lentas. Os sinais de negociação são gerados quando essas linhas se cruzam.

## Indicadores

- Cinco indicadores DeMarker com diferentes períodos: 8, 21, 34, 55 e 89.
- Cada valor do indicador é multiplicado por um fator de peso (0.05, 0.10, 0.16, 0.26 e 0.43).
- Os valores ponderados são somados para formar a linha **rápida**.
- A linha **lenta** é uma versão suavizada exponencialmente da linha rápida controlada pelo parâmetro `Smoothing`.

## Lógica de Negociação

1. Subscrever velas com um período configurável.
2. Em cada vela finalizada, calcular as linhas rápida e lenta.
3. Quando a linha rápida anterior está acima da linha lenta anterior e a linha rápida atual cai abaixo da linha lenta:
   - Fechar posições vendidas se permitido.
   - Abrir uma posição comprada se habilitado.
4. Quando a linha rápida anterior está abaixo da linha lenta anterior e a linha rápida atual sobe acima da linha lenta:
   - Fechar posições compradas se permitido.
   - Abrir uma posição vendida se habilitado.
5. Porcentagens opcionais de stop-loss e take-profit são aplicadas para posições recém-abertas.

## Parâmetros

- `CandleTimeframe` – período para subscrição de velas.
- `Smoothing` – fator de suavização para a linha lenta.
- `Factor1`–`Factor5` – fatores de peso para cada período de DeMarker.
- `DeMarkerPeriod1`–`DeMarkerPeriod5` – períodos para indicadores DeMarker.
- `Volume` – volume da ordem.
- `OpenBuy` / `OpenSell` – habilitar entradas compradas/vendidas.
- `CloseBuy` / `CloseSell` – habilitar saídas para posições compradas/vendidas.
- `StopLossPct` / `TakeProfitPct` – gestão de risco opcional baseada em porcentagem.

## Notas

A estratégia opera apenas em velas fechadas e usa a API de alto nível do StockSharp (`SubscribeCandles` e `Bind`). Todos os comentários no código são fornecidos em inglês para clareza.
