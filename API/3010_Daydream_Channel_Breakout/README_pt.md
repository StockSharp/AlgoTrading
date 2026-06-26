# Estratégia de Rompimento de Canal Daydream
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Daydream Channel Breakout é uma conversão direta do consultor especialista original do MetaTrader "Daydream" para o framework de estratégias de alto nível do StockSharp. A lógica opera contra movimentos extremos: quando o preço perfura a banda inferior do canal Donchian, o algoritmo compra esperando uma recuperação; quando o preço se estende acima da banda superior, abre exposição vendida. Todas as saídas são gerenciadas por meio de um take profit "virtual" expresso em pips, portanto nenhuma ordem nativa permanece no livro da bolsa.

## Lógica da Estratégia

- Construir um canal Donchian a partir das `ChannelPeriod` velas concluídas anteriores (a barra atual é excluída, correspondendo à implementação do MT5).
- Entrar **comprado** quando o preço de fechamento cai abaixo da banda inferior anterior. A exposição vendida existente é nivelada implicitamente porque o volume da ordem inclui o tamanho absoluto da posição.
- Entrar **vendido** quando o preço de fechamento rompe acima da banda superior anterior. A exposição comprada existente é fechada da mesma forma.
- Apenas uma entrada por vela é permitida. Após o envio de uma ordem, a estratégia aguarda a abertura da próxima barra para gerar um novo sinal.
- Cada posição aberta é monitorada para um alvo de lucro virtual. Quando o lucro não realizado excede `TakeProfitPips` (convertido para distância de preço através da heurística do tamanho do pip), a posição é fechada a mercado.

## Parâmetros

| Nome | Descrição | Padrão | Notas |
| --- | --- | --- | --- |
| `OrderVolume` | Tamanho do lote enviado com cada nova operação. O valor real da ordem também inclui o valor absoluto da posição oposta para nivelar antes de reverter. | `0.1` | Corresponde ao tamanho de lote padrão do MT5. |
| `TakeProfitPips` | Distância de take profit virtual expressa em pips. | `50` | O tamanho do pip é derivado de `Security.PriceStep`; instrumentos de 3 ou 5 dígitos são automaticamente multiplicados por 10. |
| `ChannelPeriod` | Número de velas concluídas usadas para calcular o canal Donchian. | `25` | Usa o mesmo período de retrospectiva que o EA original. |
| `CandleType` | Tipo de vela assinado para os cálculos. | `TimeSpan.FromHours(1).TimeFrame()` | Pode ser alterado para qualquer tipo de vela do StockSharp. |

## Fluxo de Sinais

1. **Assinatura de dados**: a estratégia assina o tipo de vela fornecido pelo parâmetro `CandleType` e vincula um indicador de canal Donchian usando `BindEx`.
2. **Verificação de take profit virtual**: a primeira ação em cada vela finalizada é medir a distância entre o preço de fechamento e o preço médio de entrada. Se o limiar for atingido, a posição é fechada e nenhuma nova entrada é avaliada para aquela barra.
3. **Atualização do canal**: assim que as bandas superior e inferior estiverem disponíveis, os valores anteriores são armazenados em cache para espelhar a lógica "shift=1" do MQL. Os sinais usam a banda anterior, não a atualizada com a vela atual.
4. **Decisão de entrada**:
   - Preço < banda inferior anterior → comprar `OrderVolume + Math.Max(0, -Position)`.
   - Preço > banda superior anterior → vender `OrderVolume + Math.Max(0, Position)`.
5. **Registro e visualização**: mensagens de registro informativas são produzidas para cada entrada e saída por take profit. Se uma área de gráfico estiver disponível no Designer ou em outros produtos StockSharp, as velas, o canal Donchian e as operações são desenhados automaticamente.

## Gestão de Risco

- Apenas um take profit virtual está implementado. Não existe stop-loss ou saída trailing no algoritmo original, portanto o risco deve ser controlado externamente (por exemplo, com proteções no nível de portfólio).
- Como as ordens revertem somando a posição absoluta, a estratégia pode piramidizar na mesma direção se sinais consecutivos aparecerem em diferentes velas.
- O auxiliar de tamanho de pip multiplica o passo de preço por dez para símbolos de 3 ou 5 dígitos para emular a conversão `Point()` para pip do MT5. Para instrumentos com tamanhos de tick não convencionais, você pode substituir a lógica ou usar uma distância personalizada ajustando `TakeProfitPips`.

## Notas de Uso

- A estratégia é voltada para o comportamento de reversão à média. Funciona melhor em mercados em faixa onde movimentos sobreestendidos tendem a reverter.
- Os backtests devem incluir configurações realistas de spread e comissão, pois as entradas ocorrem em ordens de mercado após rompimentos do canal.
- Considere combinar a estratégia com filtros de sessão ou stops baseados em volatilidade ao operar em bolsas ao vivo.
- A implementação depende exclusivamente da API de alto nível do StockSharp (sem coleções de indicadores manuais ou downloads históricos), portanto é compatível com Designer, Shell e Runner imediatamente.
