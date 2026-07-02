# Estratégia MultiBreakout V001k
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia MultiBreakout V001k reproduz o clássico consultor especialista MT4 "Multibreakout_v001k". Ele negocia rompimentos da sessão horária anterior empilhando ordens de compra e parada de venda assim que a hora de referência terminar. O gerenciamento de posição segue a lógica original de take-profit e ponto de equilíbrio, incluindo o ponto de equilíbrio móvel opcional que rastreia as paradas usando os mínimos/máximos horários mais recentes.

## Regras de negociação
1. **Hora de referência** – Podem ser definidos até quatro pregões. Após o fechamento de cada hora de sessão habilitada, a estratégia mede a vela horária finalizada e prepara os pedidos para a próxima hora.
2. **Colocação de entrada** –
   - As ordens buy-stop são posicionadas na máxima da hora anterior mais o spread atual e um buffer de entrada adicional (`PipsForEntry`).
   - As ordens sell-stop são posicionadas na mínima da hora anterior, menos o buffer de entrada.
   - Cada lado coloca `NumberOfOrdersPerSide` ordens pendentes com volume idêntico.
3. **Escada de lucro** – Cada entrada recebe uma meta de lucro individual espaçada por `TakeProfitIncrement` pontos. Quando o mercado atinge cada nível, a estratégia fecha uma tranche no mercado para imitar a fila original de take-profit do MT4.
4. **Gerenciamento de stop-loss** – Um stop inicial é definido a `StopLoss` pontos do preço de entrada. Assim que o preço se move `BreakEven` pontos a favor, o stop salta para o ponto de equilíbrio. Se `MovingBreakEven` estiver ativado e o atraso configurado passar, a parada segue usando as mínimas horárias mais recentes (para posições compradas) ou máximas (para posições vendidas) quando esses níveis continuarem a diminuir.
5. **Saída da sessão** – Em `ExitMinute` dentro da hora da sessão configurada, a estratégia fecha totalmente todas as posições e remove todas as ordens pendentes.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `TradeVolume` | Volume para cada pedido de breakout. |
| `NumberOfOrdersPerSide` | Quantidade de ordens pendentes empilhadas para ambas as direções. |
| `TakeProfitIncrement` | Distância (em pontos) entre metas consecutivas de take-profit. |
| `PipsForEntry` | Pontos extras adicionados ao gatilho de breakout acima/abaixo do intervalo da sessão. |
| `StopLoss` | Distância de parada inicial do preço de entrada. |
| `BreakEven` | Lucro (em pontos) necessário antes que o stop se mova para o ponto de equilíbrio. |
| `MovingBreakEven` | Ativa a lógica móvel do ponto de equilíbrio. |
| `MovingBreakEvenHoursToStart` | O atraso (em horas) após a sessão de referência antes do ponto de equilíbrio móvel pode diminuir. |
| `BrokerOffsetToGmt` | Deslocamento de horas entre o horário do corretor e o GMT usado pelo agendador de ponto de equilíbrio móvel. |
| `TradeSession1..4` | Alterna para os quatro pregões independentes. |
| `SessionHour1..4` | Hora (0-23) definindo cada sessão de referência. |
| `ExitMinute` | Minuto dentro da hora da sessão para liquidar posições e cancelar ordens. |
| `CandleType` | Tipo de vela usado para medir a hora de referência (o padrão é velas de 1 hora). |

## Notas de uso
- Certifique-se de que o instrumento tenha um `PriceStep` válido para que os cálculos de valor de pontos correspondam à versão MT4.
- A estratégia assume que os tempos do corretor estão alinhados com os carimbos de data e hora das velas. Ajuste `BrokerOffsetToGmt` quando um deslocamento de servidor MT4 diferente foi usado historicamente.
- O ponto de equilíbrio móvel avalia as duas últimas velas horárias concluídas antes de restringir o stop, correspondendo ao comportamento do consultor especialista original.
