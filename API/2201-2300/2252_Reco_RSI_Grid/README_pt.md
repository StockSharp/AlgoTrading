# Estratégia de Grade RSI Reco
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia reproduz o comportamento do consultor especialista original "Reco" do MetaTrader usando a API de alto nível do StockSharp. O algoritmo abre uma posição inicial com base no Índice de Força Relativa (RSI) e, em seguida, coloca posições contrárias formando uma grade. A distância entre as ordens da grade e seu volume crescem geometricamente. Todas as posições abertas são fechadas juntas quando o lucro ou prejuízo cumulativo atinge limiares predefinidos.

## Lógica de trading
- **Sinal inicial** – o RSI ultrapassa as zonas de sobrecompra ou sobrevenda configuradas. Uma posição vendida é aberta quando o RSI está acima do nível de venda e uma posição comprada quando está abaixo do nível de compra.
- **Expansão da grade** – após a primeira ordem, a estratégia observa o movimento do preço em relação à última negociação. Quando o preço se move uma distância calculada, uma ordem de mercado oposta é enviada. A distância aumenta com o *Distance Multiplier* em cada novo passo e pode ser limitada por *Max Distance* e *Min Distance*.
- **Escalonamento de volume** – o tamanho de cada nova ordem é igual ao *Lot* inicial multiplicado por *Lot Multiplier* elevado ao número de ordens já abertas. Limites de volume máximo e mínimo também são suportados.
- **Regras de saída** – se *Use Close Profit* estiver habilitado, todas as posições são fechadas quando o lucro agregado é maior que *Profit First Order* multiplicado por *Profit Multiplier* para cada ordem adicional. Se *Use Close Lose* estiver habilitado, a mesma lógica é aplicada às perdas usando *Lose First Order* e *Lose Multiplier*.

## Parâmetros
| Nome | Descrição |
|------|-----------|
| `RsiPeriod` | Período do indicador RSI. |
| `RsiSellZone` | Nível do RSI que aciona um sinal de venda. |
| `RsiBuyZone` | Nível do RSI que aciona um sinal de compra. |
| `StartDistance` | Distância inicial da última ordem expressa em pontos. |
| `DistanceMultiplier` | Multiplicador aplicado à distância para cada ordem adicional. |
| `MaxDistance` | Limite superior para o crescimento da distância (0 desabilita). |
| `MinDistance` | Limite inferior para o crescimento da distância (0 desabilita). |
| `MaxOrders` | Número máximo de ordens abertas simultaneamente (0 significa sem limite). |
| `Lot` | Volume base da ordem. |
| `LotMultiplier` | Multiplicador para escalonamento de volume. |
| `MaxLot` | Volume máximo permitido por ordem (0 desabilita). |
| `MinLot` | Volume mínimo permitido por ordem (0 desabilita). |
| `UseCloseProfit` | Habilitar fechamento de todas as posições por meta de lucro. |
| `ProfitFirstOrder` | Meta de lucro para a primeira ordem. |
| `ProfitMultiplier` | Multiplicador de lucro para ordens subsequentes. |
| `UseCloseLose` | Habilitar fechamento de todas as posições por limiar de perda. |
| `LoseFirstOrder` | Limiar de perda para a primeira ordem. |
| `LoseMultiplier` | Multiplicador de perda para ordens subsequentes. |
| `PointMultiplier` | Multiplicador aplicado ao passo de preço do instrumento para calcular um ponto. |
| `CandleType` | Tipo de velas usadas para cálculos do indicador. |

## Notas
- A estratégia trabalha com ordens de mercado e assume execução imediata.
- As posições são neteadas: abrir uma ordem oposta pode reduzir ou reverter a posição atual.
- A estratégia usa tabulações para indentação e comentários em inglês conforme as convenções do projeto.
