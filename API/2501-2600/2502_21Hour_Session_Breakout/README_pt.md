# Estratégia de Rompimento de Sessão de 21 Horas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia reproduz o consultor especializado "21hour" do MetaTrader dentro do StockSharp. Ela opera durante duas janelas de negociação configuráveis e usa ordens stop pendentes para capturar rompimentos no topo e na base do intervalo. No final de cada janela, a estratégia liquida qualquer exposição aberta e remove as ordens ativas, garantindo que cada dia de negociação comece zerado.

## Ideia central

- A direção das operações é determinada puramente pela ação do preço em torno dos horários de início de sessão especificados.
- No início de cada sessão, a estratégia cerca o mercado com um buy stop acima do ask atual e um sell stop abaixo do bid atual.
- Quando uma ordem stop é executada, o lado contrário é cancelado imediatamente e uma ordem de take-profit a distância fixa é colocada.
- No horário de término de sessão configurado, cada posição é fechada e todas as ordens são canceladas, mesmo que o take-profit ainda não tenha sido atingido.

## Fluxo de dados

- **Velas:** As velas de 1 minuto (configuráveis) são usadas apenas para fornecer marcas de tempo e acionar as verificações de horário.
- **Livro de ordens:** As cotações de Nível 1 fornecem os melhores valores atuais de bid/ask que definem os preços de ativação das ordens stop.

## Regras de trading

### Programação de entradas
- Às `FirstSessionStartHour` (8:00 hora do servidor por padrão) e às `SecondSessionStartHour` (22:00 por padrão), a estratégia:
  - Coloca um buy stop em `Ask + StepPoints * PriceStep`.
  - Coloca um sell stop em `Bid - StepPoints * PriceStep`.
- Apenas uma posição é permitida. Se já houver uma posição aberta quando a outra sessão começa, todas as ordens de entrada pendentes são removidas antes de novas serem colocadas.

### Gerenciamento de ordens
- Quando uma das ordens stop é executada, o stop contrário é cancelado imediatamente.
- Uma ordem de take-profit é registrada em `EntryPrice ± TakeProfitPoints * PriceStep` dependendo da direção da operação.
- Os tamanhos das ordens são fixos pelo parâmetro `Volume` (1 lote por padrão).

### Lógica de saída
- As ordens de take-profit fecham as operações vencedoras automaticamente.
- Às `FirstSessionStopHour` (21:00 por padrão) e `SecondSessionStopHour` (23:00), a estratégia fecha qualquer posição aberta a mercado e cancela todas as ordens pendentes restantes.
- Se a posição for fechada manualmente, a estratégia também remove a ordem de take-profit pendente.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `Volume` | `1` | Volume de ordem usado para entradas stop e saídas de take-profit. |
| `FirstSessionStartHour` | `8` | Hora (0-23) quando a primeira sessão de negociação começa. |
| `FirstSessionStopHour` | `21` | Hora quando a primeira sessão termina e as posições são fechadas. |
| `SecondSessionStartHour` | `22` | Hora quando a sessão da tarde começa. Deve ser após a primeira sessão. |
| `SecondSessionStopHour` | `23` | Hora quando a segunda sessão termina. Deve ser após o stop da primeira sessão. |
| `StepPoints` | `5` | Distância da melhor cotação à ordem de entrada stop, medida em passos de preço. |
| `TakeProfitPoints` | `40` | Distância entre o preço de entrada e o limite de take-profit, medida em passos de preço. |
| `CandleType` | `1 minuto` | Tipo de vela usado para acionar as verificações de horário intradiário. |

Todos os parâmetros são validados para evitar sessões sobrepostas ou combinações de horas impossíveis.

## Tags e características

- **Estilo:** Rompimento de sessão / seguidor de tendência baseado em tempo.
- **Direção:** Comprado e vendido.
- **Período:** Intradiário, impulsado por horário (velas de 1 minuto apenas para temporização).
- **Controles de risco:** Take-profit fixo mais fechamento forçado ao final da sessão (sem stop-loss).
- **Tipos de mercado:** Projetado para FX, índices, ou qualquer instrumento com horários de negociação contínuos e cotizações confiáveis.
- **Complexidade:** Baixa – sem indicadores, puramente baseado em tempo e preço.

## Notas de implementação

- A estratégia requer um `Security.PriceStep` válido; as ordens são ignoradas se o passo de preço ou as cotações não estiverem disponíveis.
- Os volumes de take-profit usam o volume da operação executada quando disponível, recorrendo à posição atual ou ao volume configurado.
- O código mantém comentários em inglês para clareza e reflete a lógica MQL original enquanto aproveita as APIs de alto nível do StockSharp (`SubscribeCandles`, `SubscribeOrderBook`, parâmetros auxiliares e helpers de ordens).
