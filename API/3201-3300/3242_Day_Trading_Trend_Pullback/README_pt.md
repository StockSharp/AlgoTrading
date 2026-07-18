# Estratégia de Day Trading Trend Pullback
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia de Day Trading é um sistema de seguimento de tendência que entra em retrações dentro de uma direção estabelecida. O consultor especialista original (entrada MQL `MQL/24298/Day Trading.mq4`) combina um filtro de tendência EMA de 100 períodos com momentum e uma confirmação MACD de período superior. O port StockSharp mantém a mesma ideia enquanto expõe cada entrada importante como um parâmetro de estratégia.

A estratégia opera em um único instrumento e um tipo de vela configurável. Nunca coloca ordens pendentes – todas as operações são executadas a mercado assim que as condições na última vela concluída são satisfeitas. Níveis protetores de stop-loss e take-profit são anexados imediatamente após a entrada.

## Lógica de trading
1. **Qualificação de tendência** – A mínima de cada uma das últimas `TrendConfirmationCount` velas deve fechar acima da EMA de 100 períodos para permitir setups comprados. Para vendidos, as máximas da janela de lookback devem permanecer abaixo da EMA. Isso reproduz o helper `candles()` do EA original.
2. **Verificação de retração** – Uma operação só pode ocorrer se pelo menos uma das três velas anteriores retraiu até a EMA de 20 períodos. Para operações compradas, a mínima deve perfurar abaixo da EMA, enquanto vendidos exigem que a mínima permaneça acima da EMA (o código MQL usava `Low > EMA20` para filtros vendidos e a mesma comparação é mantida aqui).
3. **Filtro de momentum** – O Momentum (período `MomentumPeriod`) deve desviar do valor neutro de 100 por mais de `MomentumThreshold` em qualquer uma das três últimas velas concluídas. O desvio é medido como `abs(momentum - 100)`.
4. **Confirmação MACD mensal** – O port abre posições apenas quando a linha principal do MACD mensal está acima da linha de sinal para comprados ou abaixo para vendidos. O MACD é avaliado na subscrição `MacdCandleType` (mensal por padrão) e reutiliza a configuração clássica 12/26/9.
5. **Dimensionamento de posição** – Cada nova ordem usa `Volume` lotes. O tamanho líquido da posição nunca excede `Volume * MaxPositions`. Quando o sinal se inverte enquanto há uma posição aberta, a estratégia inverte a posição combinando os volumes de fechamento e abertura em uma única ordem de mercado.
6. **Gerenciamento de risco** – Logo após uma execução, a estratégia armazena preços fixos de stop-loss e take-profit calculados a partir de `StopLossPips` e `TakeProfitPips`. Cada vela concluída verifica se algum nível foi atingido e fecha a posição se necessário.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `Volume` | Tamanho base da ordem. O valor é normalizado para o passo de volume do instrumento. | `1` |
| `CandleType` | Período de trabalho. | `TimeSpan.FromMinutes(15).TimeFrame()` |
| `MacdCandleType` | Período usado pela confirmação MACD. | `TimeSpan.FromDays(30).TimeFrame()` |
| `TrendConfirmationCount` | Número de velas que devem permanecer no lado correto da EMA de 100. Espelha o input `Count` do EA. | `10` |
| `MomentumPeriod` | Período do indicador de momentum. | `14` |
| `MomentumThreshold` | Distância absoluta mínima do momentum de 100 para permitir entradas. | `0.3` |
| `StopLossPips` | Distância de stop-loss em pips. | `20` |
| `TakeProfitPips` | Distância de take-profit em pips. | `50` |
| `MaxPositions` | Número máximo de lotes base que podem ser acumulados em uma direção. | `10` |

## Notas de implementação
- Os bindings de indicadores são realizados com a API de alto nível. A subscrição principal de velas fornece valores de EMA20/60/100 e momentum, enquanto a subscrição mensal alimenta o filtro MACD via `BindEx`.
- Todas as coleções que replicam os lookbacks do MQL (flags de retração, flags de tendência EMA, desvios de momentum) são implementadas como filas rotativas para que nenhum histórico bruto de indicador seja acessado diretamente.
- Stops e alvos são verificados em cada vela concluída. O helper que converte pips em preços adapta o tamanho do pip do instrumento `PriceStep`, reproduzindo o cálculo de `pips` usado no EA.
- A estratégia usa `StartProtection()` em `OnStarted` para que o bloco de proteção integrado esteja habilitado antes que qualquer ordem seja enviada.

## Diferenças de conversão
- O especialista original realizava inúmeras tarefas de gestão de saldo (equity stop, interruptores de break-even, trailing personalizado). Apenas as partes determinísticas da lógica de entrada/saída foram portadas. Os usuários do StockSharp podem estender a classe se essas regras de gerenciamento de dinheiro forem necessárias.
- Notificações de e-mail, push e anotações de gráfico presentes no arquivo MQL são omitidas intencionalmente.
- Como o StockSharp trabalha com posições agregadas, `MaxPositions` limita a exposição líquida absoluta em vez da contagem bruta de ordens.

## Uso
1. Anexar a estratégia a um conector que forneça o instrumento desejado e dados de velas para o período de trading e o de confirmação MACD.
2. Ajustar os parâmetros de acordo com a volatilidade do ativo e a tolerância ao risco. Aumentar `TrendConfirmationCount` ou `MomentumThreshold` torna as entradas mais seletivas.
3. Iniciar a estratégia. As ordens serão geradas automaticamente assim que todos os filtros se alinharem em uma vela concluída.

## Arquivos
- `CS/DayTradingStrategy.cs` – Implementação StockSharp.
- `README_ru.md` – Descrição em russo.
- `README_zh.md` – Descrição em chinês.
