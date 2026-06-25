# Estratégia de Gestor de Trailing Universal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia de Gestor de Trailing Universal** é uma conversão em C# do assessor especialista do MetaTrader "Universal 1.64 (edição de barabashkakvn)".
Ela automatiza tarefas de gerenciamento de operações para trading discrecional ou semiautomático, tratando entradas agendadas, ordens
pendentes em grade, trailing dinâmico para ordens de mercado e pendentes, scalping de lucro rápido e notificações no nível de portfólio
quando o capital da conta se move uma porcentagem definida.

A estratégia foi projetada para ser executada em qualquer instrumento que exponha dados de velas. Ela não depende de indicadores; em vez
disso, reage a níveis de preço e janelas de tempo, tornando-a adequada para confirmação manual de sinais ou integração em fluxos de
trabalho maiores de gerenciamento de operações.

## Principais recursos

- **Ações agendadas**: abre automaticamente posições de mercado ou coloca ordens pendentes em um horário específico do terminal (hora/minuto).
- **Grade de ordens pendentes**: mantém até uma ordem de compra limitada, venda limitada, compra stop e venda stop, cada uma com offsets
  independentes, trailing opcional e re-registro automático quando o preço se move a favor da ordem pendente.
- **Proteção de posição de mercado**: aplica lógica de stop-loss, take-profit e trailing à posição agregada atual, incluindo a opção de
  aguardar lucro não realizado antes do trailing começar.
- **Saída de scalping**: fecha posições existentes assim que o preço avança um número fixo de pontos a partir do preço médio de entrada.
- **Alertas de portfólio**: monitora o capital do portfólio e registra mensagens quando a conta cresce ou declina pelo percentual configurado.
- **Controle de posição**: suporta o modo "aguardar até que a posição seja fechada" bem como um limite configurável no número de posições
  abertas por direção antes de aceitar novas entradas ou ordens pendentes.

## Parâmetros

| Grupo | Parâmetro | Descrição |
|-------|-----------|-----------|
| Geral | `TradeVolume` | Volume de ordem em lotes usado para entradas de mercado e pendentes. |
| Geral | `WaitClose` | Quando `true`, novas ordens são permitidas somente se o número de posições abertas nessa direção estiver abaixo de `MaxMarketPositions`. |
| Mercado | `MaxMarketPositions` | Número máximo de posições ativas por direção quando `WaitClose` está habilitado. |
| Mercado | `MarketTakeProfitPoints` | Distância de take-profit (em pontos de preço) aplicada a posições abertas. Definir como 0 para desabilitar. |
| Mercado | `MarketStopLossPoints` | Distância de stop-loss (em pontos de preço) aplicada a posições abertas. Definir como 0 para desabilitar. |
| Mercado | `MarketTrailingStopPoints` | Distância de trailing stop (em pontos de preço). Definir como 0 para desabilitar o trailing. |
| Mercado | `MarketTrailingStepPoints` | Melhoria mínima (em pontos) necessária antes que o trailing stop seja movido. |
| Mercado | `WaitForProfit` | Quando habilitado, o trailing começa somente após o lucro exceder `MarketTrailingStopPoints`. |
| Mercado | `ScalpProfitPoints` | Limite de lucro (em pontos) que aciona um fechamento imediato de posição. Definir como 0 para desabilitar o scalping. |
| Pendentes | `AllowBuyLimit`, `AllowSellLimit`, `AllowBuyStop`, `AllowSellStop` | Interruptores principais para cada tipo de ordem pendente. |
| Pendentes | `LimitOrderOffsetPoints`, `StopOrderOffsetPoints` | Distância do preço de fechamento atual para colocar a ordem limitada/stop correspondente. Deve estar acima da distância mínima de stop do instrumento. |
| Pendentes | `LimitOrderTakeProfitPoints`, `StopOrderTakeProfitPoints` | Meta de lucro (pontos) anexada a posições recém-abertas criadas por ordens pendentes. |
| Pendentes | `LimitOrderStopLossPoints`, `StopOrderStopLossPoints` | Stop protetor (pontos) anexado a posições recém-abertas criadas por ordens pendentes. |
| Pendentes | `LimitOrderTrailingStopPoints`, `StopOrderTrailingStopPoints` | Distância de trailing para ordens pendentes ativas. Zero desabilita a lógica de trailing. |
| Pendentes | `LimitOrderTrailingStepPoints`, `StopOrderTrailingStepPoints` | Melhoria mínima necessária antes que uma ordem pendente seja movida durante o trailing. |
| Tempo | `UseTime` | Habilita o bloco de ação agendada. |
| Tempo | `TimeHour`, `TimeMinute` | Horário do terminal quando o bloco agendado é avaliado. |
| Tempo | `TimeBuy`, `TimeSell` | Abrir posições de compra/venda de mercado no horário agendado. |
| Tempo | `TimeBuyLimit`, `TimeSellLimit`, `TimeBuyStop`, `TimeSellStop` | Colocar a ordem pendente correspondente no horário agendado independentemente dos interruptores de permissão principais. |
| Global | `UseGlobalLevels` | Habilita o monitoramento no nível de portfólio. |
| Global | `GlobalTakeProfitPercent`, `GlobalStopLossPercent` | Limites de percentual de capital que acionam mensagens de log informativas. |
| Dados | `CandleType` | Tipo de vela usado para processamento periódico (padrão: 1 minuto). |

## Fluxo de execução

1. **Chegada de vela**: em cada vela finalizada a estratégia atualiza referências de ordens, sincroniza sinais agendados e avalia a
   lógica de trading.
2. **Janela de tempo**: se o fechamento da vela coincidir com a janela de tempo configurada, os booleanos apropriados (`TimeBuy` etc.)
   são definidos e as ordens de mercado/pendentes são registradas imediatamente.
3. **Ordens pendentes**: a estratégia coloca uma ordem pendente por tipo. Quando o movimento de preço satisfaz as regras de trailing, a
   ordem é cancelada e re-emitida mais próxima do mercado com offset preservado.
4. **Proteção de mercado**: para posições abertas a estratégia mantém ordens de stop-loss e take-profit dedicadas, ajustando-as com base
   na configuração de trailing e garantindo que os volumes correspondam à posição agregada.
5. **Verificação de scalping**: se `ScalpProfitPoints` for positivo, a posição é fechada quando o preço de fechamento atual atinge o delta
   alvo a partir do preço médio da posição.
6. **Alertas globais**: o capital do portfólio é verificado em cada ciclo; mensagens informativas são registradas assim que os limites
   são atingidos.

## Notas de uso

- Coloque a estratégia dentro de um esquema de trading onde as velas sejam entregues continuamente (por exemplo, velas de 1 minuto). A
  lógica é impulsionada por velas, portanto um período de tempo mais fino produz um trailing mais responsivo.
- A estratégia usa a propriedade `Position` agregada. Ao reverter de vendido para comprado (ou vice-versa), o tamanho da ordem executada
  é automaticamente aumentado para achatar a posição existente antes de abrir a nova.
- Os offsets de ordens pendentes e as etapas de trailing são medidos em *pontos de preço* (múltiplos de `Security.PriceStep`). Certifique-se
  de que o valor do passo do instrumento esteja configurado corretamente; caso contrário, a estratégia volta a um tamanho de passo de 1.
- O monitoramento global de lucro/perda fornece apenas mensagens de log informativas. Não fecha posições automaticamente — isso reflete o
  comportamento do assessor especialista original.
- Quando `WaitClose` está habilitado, o número de posições abertas por lado é derivado da posição agregada dividida por `TradeVolume`.
  Use tamanhos de volume consistentes para obter comportamento de controle preciso.

## Registro

Cada ação significativa — colocação de ordens, ajustes de trailing e alertas de nível global — é escrita no log da estratégia via `LogInfo`.
Monitore o log para rastrear o processo de decisão, especialmente ao ajustar offsets e parâmetros de trailing.
