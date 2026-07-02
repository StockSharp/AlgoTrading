# Estratégia de recuperação de chuva de dinheiro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Conversão do consultor especialista MetaTrader 4 **MoneyRain.mq4** para o StockSharp API de alto nível.
- Negocia no fechamento de velas finalizadas usando um filtro oscilador DeMarker.
- Mantém as saídas fixas originais de stop-loss/take-profit e o bloco de recuperação de volume que aumenta o tamanho do próximo pedido após uma sequência de perdas.

## Lógica de negociação
1. Assine o `CandleType` configurado (padrão: velas de 1 hora) e calcule o DeMarker com o período `DeMarkerPeriod`.
2. Quando nenhuma posição está ativa e nenhuma ordem está pendente:
   - Compre se o valor atual do DeMarker estiver acima de `Threshold`.
   - Venda de outra forma.
   - O tamanho do pedido é o volume base ou o volume de recuperação calculado a partir de perdas anteriores.
3. Enquanto uma posição está aberta, a estratégia observa cada vela concluída:
   - As posições compradas fecham quando a mínima da vela toca o nível de stop (`StopLossPoints` abaixo da entrada) ou a máxima da vela atinge o alvo (`TakeProfitPoints` acima da entrada).
   - Shorts refletem as mesmas regras com níveis invertidos.
4. Após cada saída, o bloco de gerenciamento de dinheiro atualiza os contadores de perdas consecutivas e prepara o próximo tamanho de pedido. Quando a seqüência de derrotas chega a `LossesLimit` a estratégia para de abrir novas posições e registra um aviso.

## Gestão de capital
- `BaseVolume` é normalizado para as regras de troca (`Security.VolumeStep`, `Security.MinVolume`, `Security.MaxVolume`). Se o tamanho normalizado cair abaixo do lote mínimo, a entrada será ignorada.
- Após cada negociação perdida, a estratégia armazena o volume utilizado (escalonado pelo lote base) e zera o contador de lucro consecutivo. A próxima negociação lucrativa usa a fórmula MoneyRain original `baseLot × lossesVolume × (StopLoss + spread) / (TakeProfit − spread)` para recuperar perdas. Os ganhos subsequentes revertem para o volume base e o acumulador de perdas é zerado após dois ou mais lucros consecutivos.
- Se `FastOptimization` estiver ativado, o bloco de recuperação será ignorado e cada entrada usará o volume base normalizado.
- O spread para a fórmula de recuperação é estimado a partir da melhor oferta/venda de nível 1 mais recente. Se as cotações não estiverem disponíveis, o spread volta a zero.

## Parâmetros
| Parâmetro | Descrição | Padrão | Notas |
|-----------|-------------|---------|-------|
| `DeMarkerPeriod` | Comprimento do oscilador DeMarker. | `10` | Deve ser maior que zero. |
| `TakeProfitPoints` | Distância até o take-profit em etapas de preço. | `50` | Convertido multiplicando por `Security.PriceStep`. |
| `StopLossPoints` | Distância até o stop loss em etapas de preço. | `50` | Deve permanecer positivo para que a fórmula de recuperação permaneça válida. |
| `BaseVolume` | Volume de pedidos de linha de base. | `1` | Normalizado para os limites do instrumento antes do envio. |
| `LossesLimit` | Máximo de negociações perdedoras consecutivas permitidas. | `1 000 000` | Quando alcançado, as entradas são pausadas até que a estratégia seja redefinida. |
| `FastOptimization` | Desative o dimensionamento de recuperação durante as execuções do otimizador. | `true` | Mantém o modelo leve para testes em massa. |
| `Threshold` | Limite DeMarker que separa os sinais de compra e venda. | `0.5` | Correspondendo à constante MT4 do código-fonte. |
| `CandleType` | Série de dados de velas usada para sinais. | `1h` | Altere para outros prazos ou agregações personalizadas. |

## Notas de uso
- Defina os valores `Security.PriceStep`, `Security.VolumeStep`, `Security.MinVolume` e `Security.MaxVolume` corretos para que as conversões de preço/volume permaneçam válidas.
- Positivos `StopLossPoints` e `TakeProfitPoints` são obrigatórios. Deixá-los em zero evita saídas, divergindo do EA original.
- A estratégia espera pelos preenchimentos reais antes de atualizar seu estado interno, portanto, ela lida com os preenchimentos parciais rastreando o preço de saída ponderado.
- Quando o limite de perda é acionado, a próxima negociação lucrativa não é realizada – reinicie ou redefina a estratégia para retomar a negociação.
