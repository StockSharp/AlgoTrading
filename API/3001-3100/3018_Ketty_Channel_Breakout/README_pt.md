# Estratégia de Rompimento de Canal Ketty
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
A Estratégia de Rompimento de Canal Ketty é uma conversão direta em C# do consultor especialista original Ketty.mq5. Ela constrói um canal de preços de curto prazo durante uma janela pré-mercado configurável e aguarda o mercado disparar para fora desse intervalo. Quando um disparo acontece, a estratégia coloca uma ordem stop no lado oposto do canal com proteção opcional de stop-loss e take-profit, refletindo o fluxo de trabalho de ordens pendentes implementado no script MQL5.

## Lógica de Trading
1. **Reinício diário** – Na primeira vela de cada dia de trading a estratégia apaga ordens pendentes (e ordens de proteção se não houver posição aberta) e reinicia as estatísticas do canal.
2. **Construção do canal** – Entre `ChannelStartHour:ChannelStartMinute` e `ChannelEndHour:ChannelEndMinute` são rastreados o máximo mais alto e o mínimo mais baixo do `CandleType` selecionado. O intervalo detectado representa o canal de rompimento para o resto do dia.
3. **Preços das ordens** – O buy stop planejado é `channelHigh + OrderPriceShiftPips`, enquanto o sell stop planejado é `channelLow - OrderPriceShiftPips`. A conversão de pip para preço coincide com o robô original: quando o instrumento tem 3 ou 5 casas decimais, um pip equivale a dez passos de preço; caso contrário, um único passo de preço é usado.
4. **Detecção de sinal** – Uma vez que o canal está disponível e a hora atual está entre `PlacingStartHour` e `PlacingEndHour`, a vela terminada mais recente é inspecionada. Uma configuração de compra aparece se a mínima da vela romper abaixo do canal pelo menos `ChannelBreakthroughPips`. Uma configuração de venda aparece quando a máxima da vela excede o canal pela mesma distância.
5. **Gestão de ordens pendentes** – Apenas uma ordem pendente está ativa a qualquer momento. Assim que um sinal é gerado, a ordem pendente anterior (se houver) é cancelada e a nova ordem stop é registrada. Todas as ordens pendentes são removidas automaticamente após `PlacingEndHour`.
6. **Ordens de proteção** – Após a execução da ordem pendente, a estratégia envia imediatamente o stop de proteção correspondente (se `StopLossPips` for positivo) e o alvo de lucro (se `TakeProfitPips` for positivo). Essas ordens são canceladas quando a posição é totalmente fechada.

## Parâmetros
- `EntryVolume` – volume padrão para novas ordens.
- `StopLossPips` – distância entre o preço de entrada e a ordem stop de proteção; definir como zero para desabilitar.
- `TakeProfitPips` – distância entre o preço de entrada e a ordem take-profit; definir como zero para desabilitar.
- `ChannelStartHour` / `ChannelStartMinute` – hora do dia em que o cálculo do canal começa.
- `ChannelEndHour` / `ChannelEndMinute` – hora do dia em que o cálculo do canal termina. O canal pode se estender além da meia-noite porque a implementação normaliza a janela de tempo.
- `PlacingStartHour` – hora do dia a partir da qual ordens pendentes podem começar a aparecer.
- `PlacingEndHour` – hora do dia após a qual todas as ordens pendentes são canceladas.
- `ChannelBreakthroughPips` – buffer de rompimento que deve ser penetrado pela última vela antes que uma ordem stop seja ativada.
- `OrderPriceShiftPips` – deslocamento adicional adicionado à borda do canal ao colocar a ordem stop pendente.
- `VisualizeChannel` – quando habilitado a estratégia desenha duas linhas horizontais que representam o canal atual no gráfico.
- `CandleType` – período usado para construir e monitorar o canal.

## Notas Adicionais
- A estratégia assume que o instrumento opera continuamente; se dados estiverem faltando dentro da janela do canal, o sistema aguardará novas velas antes de acionar qualquer ordem.
- As ordens de proteção são registradas usando ordens stop/limite separadas após o preenchimento da entrada, porque o StockSharp não anexa SL/TP diretamente a ordens pendentes da mesma forma que o MetaTrader.
- Certifique-se de que `EntryVolume` corresponda ao passo de lote do corretor e que o `CandleType` selecionado corresponda a um período líquido (o robô original foi projetado para barras de um minuto).
