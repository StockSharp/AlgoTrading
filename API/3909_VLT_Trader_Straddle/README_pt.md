# Estratégia Straddle do VLT Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia VLT Trader é uma conversão StockSharp do MetaTrader 4 consultor especialista "VLT_TRADER". A ideia original busca um período de volatilidade extremamente baixa e então prepara um rompimento em torno da vela mais recente. Quando a última vela concluída tem o menor intervalo em comparação com um número configurável de velas anteriores, as posições estratégicas param as ordens acima e abaixo dessa vela em antecipação a uma expansão de volatilidade.

## Lógica de negociação
- Assine a série de velas configurada e calcule o intervalo (máximo menos mínimo) para cada barra.
- Acompanhe o intervalo mínimo entre as barras `LookbackCandles` anteriores usando o indicador `Lowest`.
- Uma vez que a vela finalizada mais recente tenha um intervalo menor que este mínimo histórico, prepare as ordens de rompimento para a sessão seguinte.
- Coloque um stop de compra acima da máxima anterior mais `EntryOffsetPoints` e um stop de venda abaixo da mínima anterior menos o mesmo deslocamento.
- Anexe paradas e metas de distância fixa a cada ordem pendente (`StopLossPoints` e `TakeProfitPoints`).
- Deixe ambas as ordens pendentes ativas. Qualquer que seja o lado acionado primeiro, torna-se uma posição de mercado, enquanto o stop oposto permanece no livro e pode ser ativado mais tarde se o mercado reverter.
- Quando uma ordem pendente é preenchida ou cancelada, a referência correspondente é limpa para que novos straddles possam ser criados após o fechamento de todas as posições e ordens.

## Gestão de risco
- O tamanho da negociação é controlado por meio de `OrderVolume` e arredondado para a etapa e limites de volume do instrumento.
- As distâncias Stop Loss e Take Profit são expressas em etapas de preço (pontos) e convertidas em preços reais usando o `PriceStep` do instrumento.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `OrderVolume` | Tamanho do lote utilizado na criação das ordens pendentes. |
| `EntryOffsetPoints` | Pontos adicionais adicionados à máxima/mínima anterior ao colocar entradas de stop. |
| `TakeProfitPoints` | Distância de lucro associada a cada pedido. |
| `StopLossPoints` | Distância de stop loss associada a cada pedido. |
| `LookbackCandles` | Número de velas anteriores usadas para medir o intervalo histórico mínimo. |
| `CandleType` | Prazo da série de velas que alimenta a estratégia. |

## Notas
- A estratégia requer um `PriceStep` válido no instrumento; caso contrário, nenhum pedido será feito.
- Como os níveis de stop e take-profit são transmitidos juntamente com as ordens pendentes, os preços de preenchimento em StockSharp podem diferir ligeiramente de MetaTrader dependendo das regras de execução do corretor.
- A implementação depende exclusivamente de APIs de alto nível (`SubscribeCandles` + `Bind`) e do indicador `Lowest` padrão para espelhar a verificação de volatilidade do EA original.
