# Bearish Wick Reversal

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Regras de negociação

Reversão do pavio inferior, apenas comprada, em velas concluídas de um minuto. Sem posição, comprar `Volume` se `Close < Open` e `100 * (Low - Close) / Close <= Threshold`. O padrão `-1` exige pavio inferior de pelo menos um por cento do fechamento.

O horário UTC de abertura deve pertencer ao intervalo inclusivo `StartTime`–`EndTime`. Com `UseEmaFilter` ativo, também se exige `EMA(EmaPeriod)` formada e `Close > EMA`; desativado, o aquecimento da EMA não atrasa entradas.

Fechar toda a posição comprada executada quando uma vela completa fechar acima da máxima da vela completa anterior. A janela de entrada e a EMA não bloqueiam saídas. Não há vendas descobertas, espera oculta em barras, take-profit ou stop-loss automáticos. Uma ordem ativa evita duplicações.

## Parâmetros

| Parâmetro | Padrão |
|---|---|
| `Threshold` | `-1` |
| `UseEmaFilter` | `false` |
| `EmaPeriod` | `200` |
| `CandleType` | `1m` |
| `StartTime` | `2014-01-01T00:00:00Z` |
| `EndTime` | `2099-01-01T00:00:00Z` |
