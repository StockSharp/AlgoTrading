# Estratégia de Canal Plano (2684)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma conversão em C# do consultor especializado MetaTrader 5 *Flat Channel (edição barabashkakvn)*. Detecta períodos de baixa volatilidade (um canal "plano") usando o indicador de Desvio Padrão e coloca ordens stop de rompimento nos limites do canal. Quando o preço rompe o intervalo plano, a ordem stop correspondente é acionada, enquanto a oposta é cancelada para evitar ficar preso em ambos os lados do mercado.

## Lógica principal

1. **Filtro de volatilidade** – a estratégia assina velas e calcula o Desvio Padrão do preço mediano. Uma fase plana é confirmada quando o valor continua caindo por pelo menos `FlatBars` velas consecutivas.
2. **Construção do canal** – uma vez confirmada a fase plana, o máximo mais alto e o mínimo mais baixo do intervalo plano são rastreados. A largura do canal deve permanecer entre `ChannelMinPips` e `ChannelMaxPips` (convertidos para unidades de preço via o tamanho de tick do instrumento).
3. **Ordens de entrada** – enquanto o preço opera dentro do canal, a estratégia coloca:
   - Um buy stop no máximo do canal com stop-loss `2 × largura do canal` abaixo da entrada e take-profit `1 × largura do canal` acima.
   - Um sell stop no mínimo do canal com as distâncias simétricas de stop-loss/take-profit.
4. **Vida útil da ordem** – as ordens stop pendentes expiram após `OrderLifetimeSeconds`. Quando o tempo esgota, são canceladas e podem ser recriadas se as condições planas ainda se mantiverem.
5. **Gestão de posição** – após uma ordem de entrada ser executada, a ordem stop oposta é cancelada e novas ordens de proteção (stop-loss e take-profit) são registradas. A lógica opcional de ponto de equilíbrio move o stop-loss para o preço de entrada assim que o preço percorre uma fração Fibonacci (`FiboTrail`) da distância em direção ao alvo de take-profit.
6. **Janela de trading** – o filtro `UseTradingHours` restringe a atividade por dia da semana e por horas específicas de segunda/sexta-feira, emulando os controles de cronograma do EA original.

## Indicadores

- **StandardDeviation** (preço mediano, comprimento = `StdDevPeriod`) – detecta queda de volatilidade.
- **DonchianChannels** (comprimento = `FlatBars`) – fornece os limites iniciais de máximo/mínimo para o canal plano.

## Risco e gestão de capital

- `FixedVolume` define o tamanho do lote quando `UseMoneyManagement` está desabilitado.
- Quando `UseMoneyManagement` está habilitado, o tamanho da posição é estimado a partir de `RiskPercent` do valor atual do portfólio dividido pela distância do stop-loss expressa em dinheiro usando `PriceStep` e `StepPrice`.
- Após uma operação perdedora, a próxima posição usa `FixedVolume × 4`, replicando o comportamento de recuperação do EA original.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `UseTradingHours` | Habilitar ou desabilitar o filtro de horário por dia da semana/hora. |
| `TradeTuesday`, `TradeWednesday`, `TradeThursday` | Permitir trading em dias individuais no meio da semana (segunda e sexta-feira são sempre permitidas, mas controladas pelos limites horários). |
| `MondayStartHour`, `FridayStopHour` | Hora de início na segunda-feira e hora de corte na sexta-feira (relógio de 24h). |
| `UseMoneyManagement`, `RiskPercent`, `FixedVolume` | Opções de gestão de capital descritas acima. |
| `OrderLifetimeSeconds` | Tempo de expiração para ordens de entrada pendentes (0 = sem expiração). |
| `StdDevPeriod`, `FlatBars` | Configurações do indicador que controlam a detecção da fase plana. |
| `ChannelMinPips`, `ChannelMaxPips` | Largura de canal permitida expressa em pips (convertida usando o tamanho de tick do instrumento). |
| `UseBreakeven`, `FiboTrail` | Habilitar a lógica de ponto de equilíbrio e definir o multiplicador Fibonacci usado para acionar o ajuste do stop. |
| `CandleType` | Tipo de dados de velas ou período usado para os cálculos. |

## Notas

- A estratégia espera símbolos que exponham `PriceStep` e `StepPrice` para que os limiares baseados em pips possam ser convertidos em preços reais.
- As ordens pendentes são recriadas somente quando a volatilidade continua a cair. Se a volatilidade sobe, o estado plano é reiniciado e todas as ordens de entrada são canceladas.
- As ordens protetoras de stop e take-profit são canceladas automaticamente quando a posição fecha.

## Aviso legal

Este exemplo é fornecido apenas para fins educativos. O desempenho passado da estratégia original não garante resultados futuros. Teste e ajuste os parâmetros exaustivamente antes de implantar em mercados reais.
