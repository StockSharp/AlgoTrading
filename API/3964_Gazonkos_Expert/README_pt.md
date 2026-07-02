# Estratégia especializada Gazonkos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma versão StockSharp do MetaTrader 4 consultor especialista "gazonkos expert" que foi projetada para o gráfico EUR/USD H1. O EA espera por um forte movimento de impulso de uma hora e, em seguida, entra na direção desse movimento após um retrocesso configurável. Os níveis protetores de stop loss e takeprofit são aplicados como distâncias fixas medidas em pips.

## Lógica MQL4 original
- O EA monitora continuamente a diferença entre dois fechamentos históricos (`Close[t2] - Close[t1]`). Os padrões são `t1 = 3` e `t2 = 2`, que correspondem aos fechamentos das velas que terminaram há duas e três horas.
- Um impulso de alta é detectado quando `Close[t2] - Close[t1]` excede `delta` pontos. Um impulso de baixa é detectado quando `Close[t1] - Close[t2]` excede o mesmo limite.
- Assim que um impulso é detectado, o EA registra o lance mais alto (para alta) ou mais baixo (para baixa) que ocorre antes do início da próxima hora. Se o preço retroceder `Otkat` pontos a partir desse extremo na mesma hora, uma ordem de mercado será enviada na direção do impulso.
- As negociações são bloqueadas quando já existe uma posição aberta com o mesmo número mágico ou quando uma negociação já foi aberta durante a hora atual.
- Cada pedido é enviado com uma distância fixa de take-profit (`TakeProfit`) e stop loss (`StopLoss`) expressa em pontos.

## Máquina de estado na versão C#
A implementação StockSharp recria a máquina de estado original:
1. **WaitingForSlot** – verifica se nenhuma negociação recente foi aberta na hora atual e se o número máximo configurado de negociações simultâneas não foi atingido.
2. **WaitingForImpulse** – verifica os fechamentos históricos para detectar impulsos de alta ou baixa.
3. **MonitoringRetracement** – monitora os máximos/mínimos da vela após o impulso e aguarda um retrocesso de `RetracementPips` (o antigo parâmetro `Otkat`) na mesma hora.
4. **AwaitingExecution** – envia uma ordem de mercado na direção do impulso e aplica imediatamente níveis protetores de stop-loss e take-profit calculados a partir do instrumento `PriceStep`.

A estratégia processa apenas velas concluídas do período configurado e ignora dados inacabados, refletindo como o EA original avaliou as condições nas barras horárias fechadas.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `TakeProfitPips` | Distância entre o preço de entrada e o nível de lucro. |
| `RetracementPips` | Recuo necessário do extremo do impulso antes de entrar. |
| `StopLossPips` | Distância entre o preço de entrada e o stop de proteção. |
| `T1Shift` | Índice do fechamento de referência mais antigo usado para detecção de impulso (padrão 3). |
| `T2Shift` | Índice do fechamento de referência mais recente usado para detecção de impulso (padrão 2). |
| `DeltaPips` | Distância mínima do momento que deve separar os dois fechamentos de referência. |
| `LotSize` | Volume fixo de cada pedido. |
| `MaxActiveTrades` | Número máximo de negociações simultâneas; valores acima de um exigem que a conta da corretora suporte posições líquidas aditivas. |
| `CandleType` | Prazo das velas utilizadas para avaliar as regras de negociação (o padrão é 1 hora). |

Todas as distâncias baseadas em pip são convertidas em compensações de preço usando `Security.PriceStep`. Quando o instrumento não possui informações de variação de preço, um valor padrão de 0,0001 é usado, correspondendo à configuração original do EUR/USD.

## Notas de implementação
- A estratégia funciona com a assinatura de vela de alto nível de StockSharp API (`SubscribeCandles().Bind`).
- Os preços fechados são armazenados em cache em um buffer contínuo leve para emular pesquisas `Close[i]` da versão MQL4.
- Após a execução de uma negociação, a estratégia registra a hora da vela e bloqueia novas entradas até a próxima hora, reproduzindo a salvaguarda `LastTradeTime` original.
- `MaxActiveTrades` é interpretado em relação à posição líquida atual. Nas contas de compensação, isso limita efetivamente o sistema a uma única negociação aberta, que corresponde ao comportamento padrão do especialista MQL4.
- Os comentários dentro do código descrevem a máquina de estado C# em inglês para facilitar a manutenção.
