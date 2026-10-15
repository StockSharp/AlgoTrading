# Estratégia de Fogo Rápido RoNz
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina uma média móvel com o indicador Parabolic SAR para detectar mudanças rápidas de tendência. Uma posição comprada é aberta quando o preço de fechamento sobe acima da média móvel enquanto o Parabolic SAR vira para abaixo do preço. Uma posição vendida é aberta nas condições opostas. As posições podem opcionalmente ser médias quando a tendência continua.

## Como Funciona
- **Entrada comprado**: Preço de fechamento > SMA e Parabolic SAR muda para abaixo do preço.
- **Entrada vendido**: Preço de fechamento < SMA e Parabolic SAR muda para acima do preço.
- **Fechamento**: Por stop loss/take profit ou por sinal oposto dependendo do modo selecionado.
- **Médias**: Adiciona novas posições quando a tendência persiste.
- **Trailing Stop**: Ajusta o preço de stop à medida que a operação avança no lucro.

## Parâmetros
- `Volume` – volume da operação.
- `StopLoss` – stop loss em ticks.
- `TakeProfit` – take profit em ticks.
- `TrailingStop` – trailing stop em ticks.
- `Averaging` – ativar médias de posições.
- `MaPeriod` – período da média móvel.
- `PsarStep` – passo do Parabolic SAR.
- `PsarMax` – valor máximo do Parabolic SAR.
- `CloseType` – `SlClose` usa apenas stops, `TrendClose` fecha em tendência oposta.
- `CandleType` – série de candles para cálculos.

## Notas
- Funciona com qualquer instrumento suportado pelo StockSharp.
- Requer candles históricos para o `CandleType` selecionado.
