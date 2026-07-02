# Estratégia de tendência plana
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de tendência plana** reproduz as ideias centrais do consultor especialista Flat Trend original, combinando filtros de tendência de várias velocidades, confirmação de ADX e um filtro de quebra de desvio padrão "suco". A estratégia se concentra em detectar o momento em que o preço sai de uma fase de variação e o impulso se expande, permitindo-lhe unir movimentos direcionais com proteção de posição dinâmica.

## Lógica de negociação
1. **Filtros de tendência** – três médias móveis exponenciais (EMAs) com comprimentos configuráveis representam o gatilho, o primeiro filtro e o segundo filtro. Sua inclinação e a posição do preço em relação a cada EMA são traduzidas em estados:
   - Forte alta (preço acima de EMA e EMA subindo).
   - Alta moderada (preço acima de EMA, mas inclinação neutra).
   - Forte baixa (preço abaixo de EMA e EMA caindo).
   - Baixa moderada (preço abaixo de EMA, mas inclinação neutra).
2. **Regras de entrada**
   - As negociações longas exigem estados de alta no gatilho e no filtro EMA. O segundo filtro pode ser opcionalmente ignorado. O modo estrito força o uso apenas de estados fortes de alta.
   - As negociações curtas refletem as condições para estados de baixa.
   - A confirmação opcional ADX garante que o Índice Direcional Médio exceda um limite e, quando ativado, os componentes +DI e –DI concordam com a direção da negociação.
   - O filtro “juice” verifica se o desvio padrão dos preços está acima de um nível de rompimento definido pelo usuário, evitando negociações durante fases de volatilidade plana.
   - A negociação pode ser restrita a uma janela intradiária selecionada.
3. **Regras de saída**
   - Estados de tendência opostos no gatilho EMA iniciam uma saída. No modo estrito, a estratégia aguarda o contra-sinal mais forte.
   - A dinâmica para as posições de saída sempre que o preço atinge o nível de parada calculado.

## Gestão de risco
- **Parada inicial** – calculada a partir de uma distância de pip estática ou do valor Average True Range (ATR), emulando a lógica baseada em ADR do EA original.
- **Trailing stop** – movimentos com o preço mais alto (ou mais baixo) desde a entrada usando o ATR multiplicado por um divisor.
- **Ponto de equilíbrio** – quando o preço avança pela distância configurada, o stop se move além do preço de entrada em um pequeno valor de bloqueio.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `TriggerLength` | Comprimento de EMA para o filtro do acionador. |
| `FilterLength1` | Comprimento EMA para o primeiro filtro de confirmação. |
| `FilterLength2` | Comprimento EMA para o segundo filtro de confirmação. |
| `UseOnlyPrimaryIndicators` | Use apenas o gatilho e o primeiro filtro para entradas. |
| `IgnoreModerateForEntry` | Exigir estados de tendência fortes para novas negociações. |
| `IgnoreModerateForExit` | Exigir contra-sinais fortes para fechar negociações. |
| `UseTradingHours` | Habilite a janela de negociação intradiária. |
| `TradingHourBegin` / `TradingHourEnd` | Hora de início e término da janela de negociação. |
| `UseJuiceFilter`, `JuicePeriod`, `JuiceThreshold` | Parâmetros de filtro de ruptura de desvio padrão. |
| `UseAdxFilter`, `AdxPeriod`, `AdxThreshold`, `UseDirectionalFilter` | ADX força e confirmação DI. |
| `UseAdrForStop`, `StopLossPips` | Configuração inicial de stop-loss. |
| `TrailingDivisor` | Multiplicador ATR para cálculo do trailing stop. |
| `BreakEvenPips`, `BreakEvenLockPips` | Ativação do ponto de equilíbrio e distância de bloqueio. |
| `AtrPeriod` | Lookback ATR usado para estimativa de volatilidade. |
| `CandleType` | Prazo da vela primária. |

## Resumo do Indicador
- **Média Móvel Exponencial (EMA)** – três instâncias para avaliação de tendências em múltiplas velocidades.
- **Desvio Padrão** – modela o filtro de quebra de volatilidade "suco".
- **Average True Range (ATR)** – mede a volatilidade para stops e trailing.
- **Índice direcional médio (ADX)** – confirma a força e a direção da tendência.

## Notas de uso
1. Garantir que a segurança da estratégia tenha um `PriceStep` definido; caso contrário, a etapa padrão de 0,0001 será usada para distâncias baseadas em pip.
2. A estratégia usa ordens de mercado (`BuyMarket`, `SellMarket`) e dimensiona automaticamente o volume ao reverter posições.
3. As paradas dinâmicas são simuladas internamente fechando posições quando o nível de parada virtual é tocado.
4. Combine a janela de negociação e opções de entrada rigorosas para se concentrar em sessões de alta liquidez e evitar períodos agitados.
