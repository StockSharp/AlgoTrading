# Estratégia Exp Digital MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Exp Digital MACD recria o comportamento do consultor especialista original do MetaTrader 5 "Exp_Digital_MACD" dentro do framework StockSharp. O sistema escuta candles completados de um período dedicado e reage à posição relativa e inclinação de um oscilador estilo MACD. Quatro modos de operação reproduzem as regras de decisão do código fonte:

1. **Breakdown** – opera transições da linha zero do oscilador.
2. **MACD Twist** – observa uma reversão na inclinação da linha MACD.
3. **Signal Twist** – usa a curva da linha de sinal em si como confirmação.
4. **MACD Disposition** – procura pelo histograma MACD cruzar acima ou abaixo de sua linha de sinal.

Como o StockSharp não fornece o filtro proprietário "Digital MACD", a estratégia emprega o indicador padrão `MovingAverageConvergenceDivergenceSignal`. Os valores padrão (EMA rápida 12, EMA lenta 26, sinal 5) aproximam a configuração original onde o comprimento de suavização do sinal era igual a cinco. A estratégia processa apenas candles finalizados e mantém um histórico deslizante curto em campos privados para espelhar o comportamento `SignalBar = 1` da implementação MQL.

## Parâmetros
- **Mode** – seleciona um dos quatro algoritmos de trading descritos acima. Padrão: `MacdTwist`.
- **FastPeriod** – comprimento da EMA rápida usada pelo MACD. Padrão: `12`.
- **SlowPeriod** – comprimento da EMA lenta usada pelo MACD. Padrão: `26`.
- **SignalPeriod** – comprimento da EMA de suavização do sinal. Padrão: `5` para corresponder ao consultor especialista original.
- **CandleType** – período para a assinatura de candle. Padrão: candles de `4h`.
- **OrderVolume** – número de contratos ou lotes enviados em cada ordem de mercado.
- **StopLossPoints / TakeProfitPoints** – compensações de proteção expressas em passos de preço do ativo. São ativadas quando o ativo expõe um valor `Step` válido; definir como zero para desabilitar.
- **EnableLongEntry / EnableShortEntry** – alternadores que permitem ou proíbem a abertura de novas posições compradas ou vendidas.
- **EnableLongExit / EnableShortExit** – alternadores que permitem à estratégia fechar posições existentes na direção correspondente.

## Lógica de trading
O algoritmo trabalha sobre o valor de fechamento de cada candle:

- **Breakdown**: Se o valor MACD de duas barras atrás estava acima de zero, a estratégia opcionalmente fecha posições vendidas e abre uma operação comprada quando a barra seguinte cai de volta a zero ou abaixo. Inversamente, quando o MACD de duas barras atrás estava abaixo de zero, o sistema fecha comprados e abre vendidos se a barra seguinte sobe para a linha zero ou acima. Isso espelha a lógica contrária à linha zero no consultor especialista.
- **MACD Twist**: Acompanha três leituras sequenciais de MACD. Um sinal comprado aparece quando a linha forma um mínimo local (value[2] > value[1] e value[0] > value[1]). Um máximo local gera um sinal vendido. As saídas seguem o twist oposto.
- **Signal Twist**: Aplica a mesma detecção de ponto de viragem ao buffer da linha de sinal.
- **MACD Disposition**: Trabalha com os buffers MACD e de sinal. Se o MACD anteriormente estava acima da linha de sinal mas a observação seguinte cai de volta para ela ou abaixo, a estratégia entra comprada e fecha vendidas. A transição oposta leva a entradas vendidas e saídas compradas.

Cada entrada usa uma ordem de mercado com tamanho `OrderVolume + |posição atual|` para que uma reversão feche a exposição existente e estabeleça uma nova posição em uma única instrução. Sinais de saída emitem ordens de mercado que apenas aplainam a posição aberta.

## Gerenciamento de risco
`StartProtection` é habilitado uma vez que a estratégia inicia. Quando `StopLossPoints` ou `TakeProfitPoints` estão definidos acima de zero e o passo do ativo é conhecido, as ordens de proteção correspondentes são configuradas em termos absolutos de preço. Manter os parâmetros em zero desabilita a proteção automática.

## Notas de implementação
- A estratégia avalia apenas o candle completado mais recente, equivalente a `SignalBar = 1` na versão MQL.
- A implementação de MACD do StockSharp difere do Digital MACD proprietário. Os usuários podem ajustar os comprimentos de EMA para aproximar melhor o comportamento original se desejado.
- Todos os comentários dentro do arquivo fonte C# são fornecidos em inglês conforme solicitado.

## Uso
1. Anexar a estratégia a um portfólio e um ativo que fornece o período de candle necessário.
2. Ajustar os parâmetros para corresponder ao símbolo desejado e às características de volatilidade.
3. Iniciar a estratégia; ela se inscreverá automaticamente nos candles configurados, processará os valores MACD e colocará ordens de mercado de acordo com o modo selecionado.
4. Monitorar os logs ou a saída gráfica opcional para acompanhar os valores do indicador e as mudanças de posição.
