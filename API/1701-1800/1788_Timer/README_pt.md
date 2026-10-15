# Estratégia de Temporizador
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Temporizador recalcula os níveis de rompimento em intervalos de tempo fixos e opera quando o preço cruza esses limiares dinâmicos. Os níveis são posicionados usando o Average True Range (ATR) e uma distância adicional opcional em pips. A abordagem busca capturar rompimentos de curto prazo em qualquer direção.

A cada `WaitSeconds`, a estratégia define:
- **Nível de compra** em `close + pipDistance + ATR`.
- **Nível de venda** em `close - pipDistance - ATR`.

Quando o próximo candle concluído fecha além de um desses níveis, uma ordem de mercado é colocada na direção correspondente. A posição é protegida por stop-loss, take-profit e trailing stop configuráveis.

O trading pode ser limitado a uma janela de tempo específica usando as configurações de horas de negociação.

## Parâmetros
- `WaitSeconds` – segundos entre recálculos de níveis.
- `PipDistance` – distância adicional do preço atual, em pontos.
- `AtrPeriod` – período do indicador ATR.
- `TakeProfit` – distância do take-profit em pontos.
- `StopLoss` – distância do stop-loss em pontos.
- `TrailingStop` – distância do trailing stop em pontos.
- `TradeVolume` – volume da ordem.
- `CandleType` – tipo de candle para os cálculos.
- `UseTradingHours` – habilitar filtro de horário de trading.
- `StartTime` – horário de início do trading.
- `StopTime` – horário de fim do trading.

## Como funciona
1. Inscrição em candles e cálculo do ATR.
2. Em cada candle concluído:
   - Se o intervalo de tempo configurado passou, novos níveis de compra e venda são calculados.
   - Se as horas de trading estiverem habilitadas, verifica se o horário atual está dentro da janela permitida.
   - Ordem de mercado de compra ou venda é colocada se o preço cruzar o nível correspondente.
3. Stop-loss, take-profit e trailing stop são gerenciados automaticamente pela infraestrutura da estratégia.

## Notas
- A estratégia opera comprado e vendido.
- Funciona em qualquer instrumento e período.
- Os níveis baseados em ATR se adaptam à volatilidade do mercado, permitindo detecção flexível de rompimentos.
