# Estratégia SAR Trading v2.0
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia SAR Trading v2.0** recria o clássico expert advisor Cronex dentro da API de alto nível do StockSharp. Combina uma média móvel simples (SMA) com o Parabolic SAR para cronometrar as entradas e, em seguida, gerencia a posição com ordens de proteção fixas e um trailing stop baseado em pips.

- Indicadores: Média Móvel Simples, Parabolic SAR.
- Período padrão: velas de 15 minutos (configurável através de `CandleType`).
- Mercado: qualquer instrumento que forneça um valor de `PriceStep` (pip) significativo.

## Lógica de trading
- A estratégia só avalia entradas quando nenhuma posição está aberta.
- **Configuração comprada:** ou o valor do Parabolic SAR cai abaixo da SMA ou o preço de fechamento de `MaShift` barras atrás está abaixo da SMA. Isso reflete a regra MQL `SAR < MA || Close[shift] < MA`.
- **Configuração vendida:** ou o valor do Parabolic SAR sobe acima da SMA ou o fechamento de `MaShift` barras atrás está acima da SMA.
- Após enviar uma ordem de saída, o algoritmo aguarda até que a posição esteja plana antes de considerar novos sinais, correspondendo ao comportamento de posição única do EA original.

## Gestão de risco
- `StopLossPips` e `TakeProfitPips` convertem pips em distâncias de preço absolutas usando `Security.PriceStep`.
- `TrailingStopPips` mantém o stop de proteção a uma distância de pips fixa atrás do preço assim que a operação estiver em lucro.
- `TrailingStepPips` exige um buffer adicional de pips antes de mover o trailing stop novamente, emulando a lógica de "passo de trailing" do código MQL.
- Se o mercado atingir os níveis de stop-loss ou take-profit, a posição é fechada a mercado.

## Parâmetros
- `MaPeriod` (padrão **18**): número de barras usadas pela SMA.
- `MaShift` (padrão **2**): quantas barras atrás ler o preço de fechamento ao comparar com a SMA.
- `SarStep` (padrão **0.02**): fator de aceleração do Parabolic SAR.
- `SarMaxStep` (padrão **0.2**): fator máximo de aceleração do Parabolic SAR.
- `StopLossPips` (padrão **50**): distância do stop-loss fixo em pips.
- `TakeProfitPips` (padrão **50**): distância do take-profit fixo em pips.
- `TrailingStopPips` (padrão **15**): distância do trailing stop em pips.
- `TrailingStepPips` (padrão **5**): ganho adicional em pips necessário antes do trailing stop se mover novamente.
- `CandleType`: assinatura de vela usada para os cálculos.

## Notas adicionais
- A estratégia mantém um histórico interno de fechamentos para reproduzir a chamada `iClose(shift)` usada na versão MQL.
- Baseia-se exclusivamente em velas terminadas para as decisões, garantindo consistência com o expert advisor original.
- O volume é retirado da propriedade `Volume` da estratégia; por padrão, cada sinal envia uma ordem a mercado de um lote.
