# Estratégia Exp TrendValue
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma estratégia baseada no indicador TrendValue. Ela constrói bandas dinâmicas de suporte e resistência usando médias móveis ponderadas dos preços máximos e mínimos deslocadas pelo ATR. Uma nova tendência de alta ou baixa é detectada quando o preço cruza a banda oposta.

## Entrada e Saída
- **Entrada comprado**: Quando uma nova tendência de alta começa.
- **Entrada vendido**: Quando uma nova tendência de baixa começa.
- **Saída comprado**: Em um sinal de baixa ou linha de tendência.
- **Saída vendido**: Em um sinal de alta ou linha de tendência.

## Parâmetros
- `BuyPosOpen` / `SellPosOpen` – ativar entradas compradas/vendidas.
- `BuyPosClose` / `SellPosClose` – permitir o fechamento de posições compradas/vendidas.
- `StopLossPips` – stop loss em pontos de preço.
- `TakeProfitPips` – take profit em pontos de preço.
- `MaPeriod` – período da média móvel ponderada.
- `ShiftPercent` – deslocamento percentual aplicado às médias.
- `AtrPeriod` – período ATR.
- `AtrSensitivity` – multiplicador aplicado ao ATR.
- `CandleType` – período dos candles.

## Notas
A estratégia assina dados de candles e atualiza os indicadores em cada candle completado. Ordens de mercado são colocadas quando as condições são atendidas e os níveis de stop e take profit são rastreados internamente.
