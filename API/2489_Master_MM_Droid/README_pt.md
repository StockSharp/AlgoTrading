# Estratégia Master MM Droid
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Master MM Droid é um port multi-módulo do consultor especialista original do MetaTrader 5. A implementação do StockSharp mantém as ideias centrais do robô legado enquanto usa a API de alto nível para assinaturas de velas, vinculação de indicadores e gestão de ordens. Quatro blocos de gestão de capital independentes podem ser ativados ou desativados, permitindo que a estratégia misture entradas de momentum com ordens de rompimento agendadas e operações de gap semanais.

## Módulos
1. **Bloco RSI**
   - Usa um Índice de Força Relativa de 14 períodos no tipo de vela configurado.
   - Entra comprado quando o RSI cruza para cima a partir do limiar de sobrevenda e vendido quando cruza para baixo a partir do nível de sobrecompra.
   - Permite pirâmide com um número configurável de entradas adicionais separadas por um passo de preço fixo.
   - Aplica um stop inicial fixo baseado na distância em pontos e ativa um stop móvel assim que a posição está aberta.
2. **Bloco de rompimento de caixa**
   - Reconstrói caixas de rompimento três vezes por dia (horas com deslocamento de sessão 6, 12 e 20 por padrão).
   - Coloca ordens de stop agrupadas acima da máxima da sessão e abaixo da mínima com um buffer configurável.
   - Cancela todas as ordens pendentes e posições nos reinícios de sessão (horas 0, 10 e 16), imitando o comportamento do especialista original.
3. **Bloco de rompimento semanal**
   - Rastreia a ação de preço de segunda-feira e armazena a máxima/mínima acumulada da primeira parte da sessão.
   - Coloca ordens de stop simétricas dentro de uma janela de ativação limitada (`StartHour` – `WeeklySetupEndHour`) para que a semana comece com um rompimento OCO.
   - Força um estado plano nas noites de sexta-feira para evitar exposição de fim de semana.
4. **Bloco de gap**
   - Compara a nova abertura diária com a máxima/mínima do dia anterior (usando o calendário com deslocamento).
   - Compra fortes aberturas de gap de baixa e vende fortes aberturas de gap de alta.
   - Estabelece um stop protetor a uma distância configurável e entrega a gestão adicional ao motor de trailing.

## Parâmetros
| Nome | Descrição |
| ---- | --------- |
| `CandleType` | Período de tempo usado para cálculos de indicadores e verificações de janelas de tempo. |
| `TimeShiftHours` | Deslocamento de sessão aplicado aos timestamps de velas para que o horário corresponda ao EA original. |
| `StartHour` | Hora de início base da segunda-feira para o módulo semanal (antes de aplicar o deslocamento). |
| `EnableRsiModule`, `EnableBoxModule`, `EnableWeeklyModule`, `EnableGapModule` | Interruptores para os quatro blocos independentes. |
| `RsiPeriod`, `RsiLowerLevel`, `RsiUpperLevel` | Cálculo RSI e níveis de disparo. |
| `RsiMaxEntries`, `RsiPyramidPoints` | Controles de pirâmide para o bloco RSI. |
| `RsiStopLossPoints`, `RsiTrailingPoints` | Tamanhos de stop inicial e stop móvel (em pontos) para trades dirigidos por RSI. |
| `BoxEntryPoints`, `BoxTrailingPoints` | Buffer de rompimento e distância de trailing para as ordens de caixa. |
| `WeeklyEntryPoints`, `WeeklySetupEndHour`, `WeeklyTrailingPoints` | Configuração de rompimento semanal. |
| `GapStopLossPoints`, `GapTrailingPoints` | Stop protetor e distância de trailing do módulo de gap. |

Todos os parâmetros baseados em pontos são multiplicados pelo `TickSize` do instrumento para obter deslocamentos de preço, de modo que a estratégia se adapta a diferentes símbolos.

## Lógica de trading
- **Vinculação de indicadores**: Um único indicador RSI está vinculado à assinatura de velas. Cada vela concluída aciona `ProcessCandle`, que despacha os valores para os quatro manipuladores de módulo.
- **Rastreamento do estado diário**: A estratégia agrega abertura/máxima/mínima para cada dia com deslocamento para suportar a lógica de gap e manter uma referência histórica para o módulo semanal.
- **Colocação de ordens**: As ordens são enviadas através de `BuyMarket`, `SellMarket`, `BuyStop`, `SellStop` de acordo com as melhores práticas da API de alto nível. Os módulos agendados sempre cancelam as ordens ativas antes de se rearmar para evitar duplicatas.
- **Gestão de trailing**: Uma vez que uma posição está ativa, `_activeTrailingPoints` armazena a distância específica do módulo. O método `UpdateTrailing` move ordens de stop apenas na direção favorável.

## Gerenciamento de risco
- Apenas as ordens de mercado criadas pelos módulos RSI e gap são protegidas por um stop imediato calculado em pontos.
- Os módulos de rompimento dependem do motor de trailing após a ativação; podem ser combinados com proteção de portfólio externo se necessário.
- Chamar `ClosePosition()` é a forma canônica de achatar, preservando a compatibilidade com as ferramentas de risco do StockSharp.

## Notas de uso
- A estratégia opera em um único instrumento e usa o valor global `Volume` para dimensionamento. Ajuste a proteção de portfólio separadamente se precisar de limites de risco por posição.
- Os tempos de sessão são avaliados após aplicar `TimeShiftHours`. Por exemplo, com o valor padrão `2`, o reinício de caixa na hora `0` corresponde às 02:00 do servidor.
- Como as estratégias do StockSharp gerenciam posições líquidas, cestas longas/vendidas simultâneas (possíveis em contas de cobertura do MetaTrader) são consolidadas. Esta é a principal diferença de comportamento em relação ao EA original e deve ser considerada durante a validação.

## Registro e monitoramento
- Cada módulo redefine seus flags internos assim que a posição retorna a zero, ajudando os operadores a diagnosticar qual bloco produziu uma operação.
- Adicione gráficos opcionais ou registro através das instalações do StockSharp se análises detalhadas forem necessárias.
