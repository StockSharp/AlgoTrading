# Estratégia de Renda Enorme
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma versão StockSharp do MetaTrader 4 consultor especialista "Huge Income". O robô original procura movimentos intradiários que se afastem da abertura diária e entrem em uma única posição na direção do rompimento. A versão StockSharp mantém a mesma ideia, reconstruindo a faixa máxima/mínima diária a partir de velas intradiárias, abrindo apenas uma posição por vez e forçando uma saída pouco antes do fechamento do mercado configurado.

## Dados e ambiente
- **Instrumentos**: qualquer símbolo que forneça uma etapa de preço confiável (`PriceStep`). A lógica foi projetada para pares forex, mas funciona em outros instrumentos após ajustar os parâmetros do pip.
- **Prazo**: Por padrão, a estratégia assina velas de 15 minutos para reconstruir a abertura diária, máxima e mínima. Você pode mudar para um tipo de vela diferente se sua fonte de dados oferecer melhor resolução.
- **Sessões**: espera-se que o tempo do gráfico siga o relógio do corretor/servidor exatamente como o script MetaTrader. Defina os horários limite de acordo com esse fuso horário.

## Lógica de negociação
1. Reconstrua as estatísticas do dia atual sempre que uma nova vela chegar. A primeira vela do dia fornece o preço de abertura e inicializa a máxima/mínima em execução.
2. Apenas uma posição (longa ou curta) é permitida a qualquer momento. Ordens pendentes não são utilizadas; a estratégia depende de ordens de mercado.
3. **Configuração longa**:
   - O fechamento atual está acima da abertura diária.
   - A distância entre a abertura e o mínimo do dia atual é maior que `MinimumRangePips` (convertido em unidades de preço por meio de `PriceStep`).
   - A hora atual é estritamente inferior a `BuyCutoffHour`.
4. **Configuração curta**:
   - O fechamento atual está abaixo da abertura diária.
   - A distância entre a máxima do dia atual e a abertura é maior que `MinimumRangePips`.
   - A hora atual é estritamente inferior a `SellCutoffHour`.
5. Quando qualquer uma das configurações é atendida, a estratégia envia uma ordem de mercado com tamanho `TradeVolume` e para de avaliar novas entradas até que a posição fique estável novamente.
6. Após atingir o `MarketCloseHour`, qualquer posição aberta é fechada com uma ordem de mercado. Isso reflete a lógica MetaTrader que liquida as negociações perto do fechamento do fim de semana.

## Gestão de risco e dinheiro
- `TradeVolume` é o tamanho fixo do pedido. Não há comportamento de média ou martingale no script original, portanto a porta StockSharp mantém um volume constante.
- Não há níveis explícitos de stop-loss ou take-profit. O consultor especialista conta com o filtro de intervalo diário e o fechamento forçado próximo ao final da sessão para controlar o risco. Você pode estender a estratégia adicionando paradas ou lógica móvel, se necessário.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `TradeVolume` | Tamanho da posição usado ao enviar pedidos `BuyMarket` ou `SellMarket`. |
| `MinimumRangePips` | Distância mínima (em pips) entre a abertura diária e o extremo oposto antes de uma negociação ser permitida. Convertido para uma diferença de preço absoluta usando `Security.PriceStep`. |
| `BuyCutoffHour` | Última hora (0–23) em que novas entradas longas podem ser abertas. A comparação é estrita (`currentHour < BuyCutoffHour`). |
| `SellCutoffHour` | Última hora (0–23) em que novas entradas curtas podem ser abertas. |
| `MarketCloseHour` | Hora do dia em que todas as posições abertas são liquidadas. Defina-o como 23 para corresponder ao comportamento de fechamento original EA às sextas-feiras. |
| `CandleType` | Período usado para assinar velas e reconstruir estatísticas diárias. |

## Diferenças da versão MT4
- StockSharp recebe dados de velas em vez de ticks individuais. Se o feed MetaTrader do seu corretor dependesse de atualizações tick-by-tick, escolha um intervalo de vela suficientemente pequeno para emular a mesma capacidade de resposta.
- O filtro `MinimumRangePips` é desativado automaticamente quando o instrumento não possui um `PriceStep`. Nesse caso, todos os rompimentos acima/abaixo da abertura são aceitos.
- Todas as negociações são executadas com ordens de mercado e imediatamente achatadas em `MarketCloseHour`, replicando o loop `OrderClose` do código original sem ordens pendentes.

## Dicas de uso
- Ajuste o prazo da vela para corresponder à velocidade de execução de sua preferência. Velas mais curtas rastreiam a máxima/mínima diária com mais precisão, mas requerem mais dados.
- Revise o horário de negociação do instrumento. Se o mercado fechar antes do `MarketCloseHour` configurado, a saída forçada será acionada no dia de negociação seguinte.
- Combine a estratégia com proteções em nível de portfólio ou conta (por exemplo, `StartProtection`) se você precisar de limites de stop loss ou drawdown além do design original.
