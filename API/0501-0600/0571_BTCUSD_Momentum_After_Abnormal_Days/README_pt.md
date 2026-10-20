# BTCUSD Momentum After Abnormal Days

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Regras de negociação

O retorno de uma vela diária concluída é `r=(Close-Open)/Open`. Média e desvio populacional usam os `LookbackPeriod` retornos diários anteriores, excluindo o atual, e exigem janela completa. Compra se `r>mean+k*sd`, vende se `r<mean-k*sd`, com `k=AbnormalThreshold`. Igualdade não entra.

Entra somente sem posição. Quantidade: `floor(CapitalPerTrade/Close/VolumeStep)*VolumeStep`; ignora volumes abaixo do mínimo e rejeita acima do máximo. O capital usa o fechamento do sinal e a execução pode variar. Encerra toda a posição executada no fechamento diário seguinte ou no primeiro fechamento posterior disponível após uma lacuna. A saída tem prioridade sobre nova entrada nessa vela. Ordens pendentes bloqueiam duplicações; sem stop de preço.

## Parâmetros

| Parâmetro | Padrão |
|---|---|
| `CandleType` | `1d` |
| `LookbackPeriod` | `5` |
| `AbnormalThreshold` | `1.6` |
| `CapitalPerTrade` | `1000` |
