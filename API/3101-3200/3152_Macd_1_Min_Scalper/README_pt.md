# Estratégia MACD 1 MIN SCALPER
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é um port em C# do consultor especialista MetaTrader **"MACD 1 MIN SCALPER"**. Combina médias móveis ponderadas com confirmações de MACD em múltiplos períodos e um filtro de momentum antes de abrir operações. O objetivo é negociar na direção da tendência quando os indicadores de períodos inferior e superior estão alinhados e o momentum do preço é suficientemente forte.

## Lógica de Negociação

1. **Período base** – configurável (padrão M1). Duas médias móveis ponderadas (WMA) com períodos 50 e 200, calculadas sobre o preço típico `(Máxima + Mínima + Fechamento) / 3`, definem a tendência de curto prazo.
2. **Filtro de tendência de período superior** – WMAs com os mesmos períodos são calculadas no período H1. Configurações longas requerem que ambas as WMAs rápidas estejam acima de suas contrapartes lentas, as vendidas requerem o oposto. Se o período de trabalho já é H1, as WMAs base são reutilizadas.
3. **Confirmações MACD** – o MACD (12, 26, 9) deve ter sua linha principal acima da linha de sinal no período base, no período H1 e em um período mensal (aprox. 43200 minutos). Entradas vendidas requerem que todos os três MACDs estejam abaixo de seus sinais.
4. **Filtro de momentum** – um indicador de momentum com período 14 opera em um período superior derivado do período base do MetaTrader (M1→M15, M5→M30, …). O desvio absoluto de 100 deve exceder um limiar configurável em pelo menos uma das últimas três barras completadas.
5. **Regras de entrada** – uma posição longa é aberta quando todas as condições altistas são atendidas e a estratégia atualmente não tem exposição longa. Uma posição curta requer as condições baixistas refletidas. Se uma posição oposta estiver aberta, o tamanho da ordem inclui automaticamente a quantidade necessária para fechá-la.
6. **Gestão de risco** – distâncias opcionais de stop-loss e take-profit são especificadas em pips e convertidas para pontos do instrumento na inicialização. Funções de trailing, breakeven e gestão monetária do script original são intencionalmente omitidas neste port de alto nível.

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| `CandleType` | Período de trabalho para os indicadores base. |
| `OrderVolume` | Volume enviado com cada entrada de mercado. Também usado para fechar/inverter posições. |
| `FastMaPeriod` / `SlowMaPeriod` | Comprimentos das médias móveis ponderadas rápida e lenta. |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | Períodos EMA usados pelo indicador MACD. |
| `MomentumPeriod` | Comprimento do indicador de momentum no período de confirmação. |
| `MomentumThreshold` | Desvio absoluto mínimo de 100 necessário para aceitar o momentum. |
| `TakeProfitPips` / `StopLossPips` | Níveis protetores opcionais especificados em pips. |

## Notas de Implementação

- A estratégia depende das assinaturas de velas de alto nível do StockSharp (`SubscribeCandles`) e da vinculação de indicadores (`Bind` / `BindEx`). Nenhum cálculo manual de indicadores ou buffers históricos é usado.
- O período de momentum é derivado do mapeamento do MetaTrader: `[1,5,15,30,60,240,1440,10080,43200]`. Se um valor estiver fora desta lista, um multiplicador 4× do período base é usado como fallback.
- `StartProtection` é iniciado apenas quando pelo menos um dos parâmetros de risco é maior que zero. Não há implementação de trailing stop neste port.
- A renderização do gráfico está habilitada para as velas base, ambas as WMAs e o MACD para facilitar a inspeção visual durante a depuração ou negociação ao vivo.

## Dicas de Uso

- Defina o parâmetro `OrderVolume` de acordo com o tamanho mínimo de lote do instrumento. O auxiliar ajusta automaticamente o volume enviado para corresponder ao passo e às restrições de min/max do símbolo.
- Certifique-se de que os dados de períodos superiores (H1 e mensal) estejam disponíveis no feed de dados. Sem essas velas, a estratégia não abrirá posições porque os sinais de confirmação permanecem incompletos.
- A filtragem de momentum é sensível ao limiar escolhido. Valores mais altos exigem impulsos de momentum mais fortes, enquanto valores mais baixos resultam em operações mais frequentes.
