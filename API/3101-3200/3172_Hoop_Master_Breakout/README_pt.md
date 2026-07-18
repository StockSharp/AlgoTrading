# Estratégia de Hoop Master Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Convertida do assessor especialista do MetaTrader 5 **"Hoop master 2"** de Vladimir Karputov.
- Constrói uma caixa de rompimento ao redor do preço atual e arma ordens de compra e venda stop toda vez que um novo ローソク足 fecha.
- Reproduz automaticamente o comportamento do MT5 de dobrar o tamanho do lote após uma operação perdedora e redefinir após um ciclo lucrativo.

## Lógica de negociação
1. Subscrever a série de velas configurada e aguardar apenas velas concluídas. Uma nova vela age como o "tick" que re-arma as ordens pendentes.
2. Quando a estratégia está plana:
   - Colocar um **buy stop** `IndentPips` pontos acima do último fechamento.
   - Colocar um **sell stop** `IndentPips` pontos abaixo do último fechamento.
   - Converter pips do MetaTrader em unidades de preço absolutas usando o `PriceStep` do instrumento e o ajuste de dígitos fracionários (×10 para cotações de 3 ou 5 casas decimais).
3. Cada ordem pendente armazena seus próprios níveis de stop-loss e take-profit. Assim que a ordem é executada, a ordem oposta é cancelada e a proteção armazenada é recriada com ordens nativas de bolsa (`SellStop`/`SellLimit` para compradas, `BuyStop`/`BuyLimit` para vendidas).
4. Se uma ordem protetora fechar a posição, a ordem anexa restante é cancelada para evitar saídas duplicadas.
5. A lógica de Trailing stop opcional move o stop protetor a favor da operação assim que o preço avançou pelo menos `TrailingStopPips` e a melhoria excede `TrailingStepPips`.
6. Após cada ciclo de plano para plano, o PnL realizado é avaliado. Um ciclo negativo multiplica o volume de trabalho por `LossMultiplier`; caso contrário, o volume é redefinido para o `Volume` base.

## Parâmetros
| Parâmetro | Descrição | Padrão | Notas |
|-----------|-------------|---------|-------|
| `Volume` | Tamanho de ordem base usado ao armar novas ordens pendentes. | Propriedade `Volume` da estratégia | Dobra após um ciclo de perda de acordo com `LossMultiplier`. |
| `StopLossPips` | Distância de stop-loss em pips do MetaTrader. | `25` | Convertido para preço usando o auxiliar de tamanho de pip. `0` desativa o stop. |
| `TakeProfitPips` | Distância de take-profit em pips do MetaTrader. | `70` | Convertido para preço. `0` desativa o objetivo. |
| `TrailingStopPips` | Distância entre o preço e o Trailing stop. | `0` | Definir como `0` para desativar o trailing. |
| `TrailingStepPips` | Melhoria mínima antes de mover o Trailing stop. | `5` | Usado apenas quando `TrailingStopPips` for maior que zero. |
| `IndentPips` | Deslocamento adicionado ao último fechamento ao armar ordens pendentes. | `15` | Garante que as ordens stop fiquem fora do ruído de preço imediato. |
| `LossMultiplier` | Multiplicador aplicado ao próximo ciclo após uma perda. | `2` | Implementa o dimensionamento de posição estilo martingale do EA MT5. |
| `CandleType` | Tipo/período de vela que aciona o re-armamento. | `Período de 1 hora` | Alterar para corresponder ao gráfico usado nos testes. |

## Gestão de dinheiro e proteções
- Cada entrada executada imediatamente reconstrói seu stop-loss e take-profit como ordens reais de bolsa para que as proteções funcionem mesmo se a estratégia desconectar.
- `StartProtection()` é invocado durante a inicialização para liquidar posições perdidas de execuções anteriores.
- A lógica de trailing ajusta ordens stop existentes em vez de enviar saídas de mercado, mantendo o comportamento consistente com as modificações do MT5.

## Notas de implementação
- Segue a API de alto nível do StockSharp: subscrições de velas, `BuyStop`/`SellStop` para entradas e `BuyLimit`/`SellLimit` para ordens de take-profit.
- Todos os comentários textuais dentro do código estão em inglês, enquanto a documentação externa (este README e traduções) fornece descrições detalhadas para os usuários.
- A conversão de pips do MetaTrader respeita símbolos de dígitos fracionários (3 ou 5 casas decimais) multiplicando o passo do corretor por 10, correspondendo à lógica `m_adjusted_point` do EA original.
