# Entrada limite MFI com cancelamento temporizado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O diagrama somente comprado mostra o ciclo completo de uma ordem pendente. MFI(14) sai da sobrevenda, registra compra abaixo do fechamento, N values conta cinco candles, Order cancellation remove o limite e Trades for order leva execuções à proteção 1%/1%.

![schema](schema.svg)

## Visão geral da estratégia

- O cruzamento ascendente do MFI por 20 modela a visita lembrada à sobrevenda.
- Com Position == 0 registra compra de uma unidade em Close × (1 − 0,5/100).
- Um flag de ciclo único bloqueia novas ordens até terminar cinco candles, evitando que timeout antigo cancele ordem nova.
- Se executar, Trades for order alimenta a proteção; se não, o temporizador cancela exatamente a ordem registrada.

## Regras de entrada e saída

- **Entrada comprada**: MFI cruza 20 para cima com posição zerada e coloca compra 0,5% abaixo do fechamento finalizado.
- **Entrada vendida**: Não há entrada vendida, como no C#.
- **Saída**: A entrada executada sai em +1% ou −1%. A pendente é cancelada após cinco candles, contando o candle do sinal como primeiro.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candle Time Frame | 00:05:00 | Intervalo finalizado para MFI, preço e contador. |
| MFI Period | 14 | Quantidade de candles do MoneyFlowIndex. |
| MFI Oversold Level | 20 | Nível MFI cujo cruzamento ascendente arma entrada. |
| Replay Entry Offset, % | 0.5 | Distância abaixo do fechamento; C# 0,1%, replay 0,5%. |
| Order Volume | 1 | Volume da compra pendente. |
| Cancel After Candles | 5 | Candles finalizados antes do cancelamento. |
| Take Profit, % | 1 | Ganho percentual desde a execução. |
| Stop Loss, % | 1 | Perda percentual desde a execução. |

## Detalhes do diagrama

- C# usa 0,1%. O emulador executa dentro de Low..High, quase sempre imediatamente; 0,5% torna o cancelamento visível no replay.
- Trades for order é obsoleto; recomenda-se Trades de Order registering. Ele aparece exatamente uma vez aqui como lição histórica.
- O flag é liberado pelo temporizador, não pela execução; timeout antigo não consegue cancelar ordem substituta.
- O cooldown fonte é 20 candles. Foi omitido como simplificação, mas a trava de cinco candles evita sobreposição.
- Apesar do nome da pasta, o C# não faz averaging; o diagrama tem uma entrada e não inclui Chart panel.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
