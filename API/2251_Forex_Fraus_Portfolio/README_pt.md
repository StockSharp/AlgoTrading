# Estratégia de Portfólio Forex Fraus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia negocia um único instrumento com base no indicador **Williams %R** com um período longo. Quando o indicador sai de zonas extremas, a estratégia abre posições na direção do rompimento.

## Como funciona

1. Williams %R é calculado durante `WprPeriod` velas.
2. Quando o indicador cai abaixo de `BuyThreshold`, uma oportunidade de compra é preparada. Assim que ele sobe acima do limiar, uma ordem de compra de mercado é colocada.
3. Quando o indicador sobe acima de `SellThreshold`, uma oportunidade de venda é preparada. Assim que ele cai abaixo do limiar, uma ordem de venda de mercado é colocada.
4. Posições são permitidas apenas durante a janela de tempo entre `StartHour` e `StopHour`.
5. Stop loss, take profit e trailing stop opcionais podem ser ativados através de parâmetros.

## Parâmetros

- `WprPeriod` – período do Williams %R.
- `BuyThreshold` – valor para habilitar um sinal de compra.
- `SellThreshold` – valor para habilitar um sinal de venda.
- `StartHour` / `StopHour` – limites da sessão de negociação.
- `SlPoints` – stop loss em pontos. Desativado se 0.
- `TpPoints` – take profit em pontos. Desativado se 0.
- `UseTrailing` – ativar lógica de trailing stop.
- `TrailingStop` – distância de trailing em pontos.
- `TrailingStep` – passo para atualizações do trailing.
- `CandleType` – tipo de vela a subscrever.

## Notas

A versão MQL4 original negociava vários pares de divisas e geria ordens para cada um. Este porto em C# foca-se num único instrumento e demonstra a ideia central usando a API de alto nível do StockSharp.
