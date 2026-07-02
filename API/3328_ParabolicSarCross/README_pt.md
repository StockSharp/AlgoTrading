# Estratégia Parabolic SAR Cross
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é um port StockSharp do expert advisor "PSAR Trader EA" do MetaTrader. Ela observa como o preço interage com o indicador Parabolic SAR e reage apenas quando o campo de pontos vira de um lado do corpo do candle para o outro. A conversão preserva a lógica original de gestão monetária: a estratégia pode negociar com lote fixo ou ajustar dinamicamente o volume da ordem com base no saldo da conta, aplica stop-loss e take-profit fixos e ativa trailing stop quando uma operação acumula lucro suficiente.

## Lógica da estratégia
- Construir um indicador Parabolic SAR com aceleração e valores máximos definidos pelo usuário na série de candles selecionada (candles de 30 minutos por padrão).
- Detectar uma **virada altista** quando o ponto SAR se move de acima do corpo do candle para abaixo dele. Se não houver posição aberta, enviar uma ordem de compra a mercado. Se existir uma posição vendida, fechá-la primeiro e aguardar o próximo sinal para reentrar comprado.
- Detectar uma **virada baixista** quando o ponto SAR se move de abaixo do corpo do candle para acima dele. Se estiver zerado, abrir uma posição vendida. Se uma posição comprada estiver ativa, fechá-la e adiar a entrada até o sinal seguinte.
- Monitorar operações abertas em cada candle concluído e executar saídas quando qualquer nível protetor (stop-loss, take-profit ou trailing stop) for alcançado pela máxima/mínima do candle atual.

## Gestão de risco
- **Stop loss:** expresso em pontos (passos de preço). Para compras, o stop é colocado abaixo do preço de entrada; para vendas, acima.
- **Take profit:** também expresso em pontos. A meta espelha o stop na direção oposta e fecha a posição inteira quando alcançada.
- **Trailing stop:** começa depois que o preço se move por um número configurável de pontos a favor da operação. O trailing stop aperta apenas na direção do lucro, replicando o comportamento "tighten stops only" do EA original.

## Gestão de volume
- **Lote fixo:** quando auto-lote está desabilitado, a estratégia envia ordens com o lote fixo configurado.
- **Lote baseado no saldo:** quando auto-lote está habilitado, o volume é calculado como `(Account Balance / 1000) * LotsPerThousand` e alinhado ao passo de volume e volume mínimo do ativo.

## Parâmetros e padrões
- `SarStep`: fator de aceleração do Parabolic SAR. Padrão: `0.02`.
- `SarMaximum`: aceleração máxima do Parabolic SAR. Padrão: `0.2`.
- `CandleType`: timeframe para análise. Padrão: candles de 30 minutos.
- `UseAutoLot`: habilita dimensionamento dinâmico de lote. Padrão: `false`.
- `FixedLot`: volume usado quando auto lote está desligado. Padrão: `0.1`.
- `LotsPerThousand`: multiplicador para cálculos de auto-lote. Padrão: `0.05`.
- `StopLossPoints`: distância até o stop em pontos. Padrão: `500`.
- `TakeProfitPoints`: distância até o take profit em pontos. Padrão: `1000`.
- `TrailingStartPoints`: limiar de lucro que habilita trailing. Padrão: `500`.
- `TrailingDistancePoints`: offset de trailing quando habilitado. Padrão: `100`.

## Notas
- A estratégia negocia direções comprada e vendida, mas mantém no máximo uma posição aberta por vez.
- Ordens protetoras são simuladas em dados de candles; picos intrabar menores que o timeframe selecionado podem influenciar a qualidade de execução no trading ao vivo.
