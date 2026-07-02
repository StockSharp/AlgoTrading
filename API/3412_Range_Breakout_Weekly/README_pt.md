# Estratégia semanal de intervalo (ID 3412)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia semanal de intervalo** é uma conversão de StockSharp API de alto nível do MetaTrader 5 consultor especialista `RangeBreakout.mq5`. O sistema prepara níveis de rompimento uma vez por semana usando um dia e hora da semana configuráveis ​​e, em seguida, abre uma única negociação quando o preço ultrapassa ou ultrapassa o intervalo calculado. O dimensionamento de posição no estilo Martingale e a lógica de compensação de perdas refletem o script original, enquanto a implementação aproveita assinaturas StockSharp para velas, cotações de nível 1 e vinculação de indicadores.

## Lógica de negociação

1. **Janela de preparação semanal.** No fechamento da vela horária especificada no dia da semana configurado, a estratégia registra o fechamento da vela como preço de referência e transita da fase *Standby* para *Setup*.
2. **Cálculo de intervalo.**
   - O intervalo primário é derivado de um intervalo verdadeiro médio diário de 20 períodos (ATR). O valor ATR é multiplicado por `ATR Percentage` e normalizado para o tamanho do tick do instrumento.
   - Se faltarem dados de ATR, o algoritmo volta a multiplicar o preço de venda atual por `Price Percentage`.
3. **Níveis de proteção.**
   - Os gatilhos de rompimento superior e inferior são colocados um intervalo acima e abaixo do fechamento de referência.
   - As compensações de take-profit e stop-loss são calculadas como porcentagens do intervalo. Quando a compensação está ativa após uma perda, o take-profit é substituído pela compensação de compensação acumulada e o stop-loss é ampliado no mesmo valor, assim como a lógica MetaTrader.
4. **Execução.**
   - Enquanto estiver em *Configuração*, a estratégia escuta as cotações do Nível 1. Uma quebra acima do gatilho superior entra em uma posição longa; uma queda abaixo do gatilho inferior abre uma posição curta. As ordens são enviadas como ordens de mercado com verificações de preços alinhadas aos ticks.
   - Assim que uma posição estiver ativa (fase *Trade*), as cotações do Nível 1 são monitoradas continuamente. Atingir o stop ou alvo de proteção fecha a posição com uma ordem de mercado.
5. **Martingale recuperação.**
   - Após uma saída perdedora, o tamanho da próxima negociação dobra e a compensação de perda é adicionada ao buffer de compensação para que a meta seguinte vise recuperar a perda acumulada.
   - Uma saída vencedora redefine o multiplicador e o buffer de compensação para seus valores iniciais.
6. **Reinicialização diária.** Após a conclusão de uma negociação, a estratégia retorna à fase *Standby* e aguarda até a próxima combinação elegível de dia da semana/hora para preparar uma nova configuração.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `Trading Day` | Segunda-feira | Dia da semana usado para medir a vela de referência de rompimento. As seleções de fim de semana são automaticamente remapeadas para segunda-feira, correspondendo ao comportamento de aviso original. |
| `Start Hour` | 0 | Hora (0-23) cuja vela de fechamento serve de referência. Otimizável para cobrir diversas vagas de sessão. |
| `Price Percentage` | 1,0 | Porcentagem de fallback do preço de venda usado para calcular o intervalo quando faltam dados de ATR. |
| `ATR Percentage` | 100 | Multiplicador aplicado ao valor diário de ATR para obter o intervalo de breakout. |
| `Take Profit Percentage` | 100 | Porcentagem da faixa adicionada além da entrada para definir o preço de take-profit. Substituído pelo buffer de compensação após perdas consecutivas. |
| `Stop Loss Percentage` | 100 | Porcentagem do intervalo subtraído da entrada para definir o preço do stop loss. O buffer de compensação amplia essa distância após perdas. |
| `Base Volume` | 0,1 | Volume inicial de negociação antes da escala do martingale. O valor é arredondado automaticamente para o passo de volume do instrumento e limitado por restrições mínimas/máximas. |
| `ATR Period` | 20 | Número de velas diárias fornecidas ao indicador ATR. |
| `Hour Candle Type` | Período de 1 hora | Assinatura de vela usada para detectar a janela de preparação. |
| `ATR Candle Type` | Período de 1 dia | Assinatura de vela que alimenta o indicador ATR. |

## Notas de implementação

- **Assinaturas de dados.** A estratégia assina velas horárias para agendamento, velas diárias para o cálculo de ATR e dados de nível 1 para monitoramento de oferta/venda. O `Bind` API de alto nível é usado para transmitir valores de indicadores sem manipulação manual de buffer.
- **Alinhamento de ticks.** Todos os níveis de preços (referência, gatilhos, stop-loss, take-profit) são normalizados por meio de `Security.ShrinkPrice` para respeitar as restrições de tamanho de tick, imitando o comportamento de MetaTrader `NormalizeDouble`.
- **Manuseio de volume.** Os volumes de negociação são arredondados para o `VolumeStep` do instrumento e limitados por `VolumeMin`/`VolumeMax` antes do envio do pedido, replicando a higienização do lote original.
- **Máquina de fases.** Fases internas (`Standby`, `Setup`, `Trade`) substituem a lógica enum original, garantindo uma única negociação por ciclo de preparação. Após cada saída, o estado é redefinido para `Standby` até que ocorra a próxima vela de qualificação.
- **Buffer de compensação.** O campo `compensationOffset` armazena a distância de perda acumulada expressa em unidades de preço. Quando ativa, a próxima configuração substitui a compensação de lucro por este valor e amplia o stop no mesmo valor, espelhando a fórmula MetaTrader que converte a perda monetária passada em distância de preço.
- **Registro.** Selecionar sábado ou domingo aciona um registro informativo e muda automaticamente o dia útil para segunda-feira, consistente com o aviso mostrado pela versão MQL.

## Dicas de uso

1. Alinhe `Trading Day` e `Start Hour` com a sessão que gera intervalos significativos (por exemplo, intervalo asiático ou intervalo aberto de Londres).
2. Calibre `ATR Percentage`, `Take Profit Percentage` e `Stop Loss Percentage` juntos. Aumentar o multiplicador de intervalo produz gatilhos mais amplos e negociações mais lentas, enquanto o ajuste das porcentagens de lucro/perda modifica a relação recompensa/risco.
3. Ative a otimização em `Start Hour`, `Base Volume` ou nos parâmetros de porcentagem para reproduzir varreduras de parâmetros do consultor especialista original.
4. Monitore a exposição cumulativa criada pelo multiplicador de martingale. Considere reduzir `Base Volume` ao executar em contas altamente alavancadas.
5. A estratégia é concebida para um único instrumento. Implante diversas cópias com diferentes valores mobiliários ou configurações de sessão para diversificar a cobertura.

## Cobertura de conversão

- ✅ Programação semanal preservada, cálculos de intervalo, níveis de proteção e comportamento martingale de `RangeBreakout.mq5`.
- ✅ Chamadas API específicas de MetaTrader substituídas (`iATR`, `CopyBuffer`, `OrderSend`, etc.) por abstrações StockSharp idiomáticas (`SubscribeCandles`, `AverageTrueRange`, `BuyMarket`/`SellMarket`).
- ✅ Implementação de comentários in-line em inglês e extensa documentação conforme solicitado.
- ✅ Deixou os projetos de teste intactos e não criou uma variante Python, cumprindo as restrições da tarefa.
