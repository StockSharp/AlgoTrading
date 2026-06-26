# FT CCI MA (Port do StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
Esta estratégia é um port direto do consultor especializado MetaTrader "FT CCI MA". Opera no fechamento de cada candle terminado, combinando uma média móvel ponderada linearmente (LWMA) com limiares do Commodity Channel Index (CCI) e um filtro de sessão de negociação opcional. A implementação StockSharp mantém os mesmos nomes de parâmetros e valores padrão, permitindo reproduzir o comportamento original enquanto se beneficia da API de alto nível (assinaturas de candles, vinculação de indicadores, proteção de posição).

Notas chave de design:
- A LWMA trabalha sobre o preço ponderado `(High + Low + 2 * Close) / 4`, correspondendo ao modo `PRICE_WEIGHTED` do MetaTrader.
- O CCI usa o preço típico `(High + Low + Close) / 3`, como em `PRICE_TYPICAL`.
- Todas as decisões são avaliadas na barra recém-fechada, o que reflete o EA original que aguardava o início da próxima barra antes de agir sobre a anterior.
- A proteção de posição replica o take-profit e stop-loss do EA em unidades de pip.

## Regras de negociação
1. **Entradas compradas**
   - Preço de fechamento acima da LWMA e CCI abaixo de `CciLevelBuy` (padrão -100), *ou*
   - Preço de fechamento abaixo da LWMA e CCI abaixo de `CciLevelDown` (padrão -200).
   - Entrar apenas se a posição líquida atual estiver zerada ou vendida.
2. **Entradas vendidas**
   - Preço de fechamento abaixo da LWMA e CCI acima de `CciLevelSell` (padrão 100), *ou*
   - Preço de fechamento acima da LWMA e CCI acima de `CciLevelUp` (padrão 200).
   - Entrar apenas se a posição líquida atual estiver zerada ou comprada.
3. **Filtro de tempo**
   - Quando `UseTimeFilter` está habilitado, a estratégia verifica a hora de `candle.CloseTime`.
   - Se a hora estiver fora da janela ativa, todas as posições e ordens são canceladas/fechadas imediatamente.
4. **Controles de risco**
   - `StartProtection` define distâncias absolutas de stop-loss e take-profit usando o tamanho de pip derivado de `Security.PriceStep`.
   - O volume da ordem é nettado de modo que abrir na direção oposta fecha automaticamente a exposição anterior.

## Parâmetros
| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `OrderVolume` | Tamanho do trade em lotes. | `1` |
| `StopLossPips` | Distância de stop-loss em pips (0 desativa). | `150` |
| `TakeProfitPips` | Distância de take-profit em pips (0 desativa). | `150` |
| `UseTimeFilter` | Habilita o filtro de sessão. | `true` |
| `StartHour` | Hora de início da sessão em tempo de exchange (0-23). | `10` |
| `EndHour` | Hora de fim da sessão em tempo de exchange (0-23). Quando menor que a hora de início, a sessão ultrapassa a meia-noite. | `5` |
| `CciPeriod` | Comprimento do Commodity Channel Index. | `14` |
| `CciLevelUp` | Limiar vendido agressivo (+200). | `200` |
| `CciLevelDown` | Limiar comprado agressivo (-200). | `-200` |
| `CciLevelBuy` | Limiar comprado suave quando o preço está acima da MA (-100). | `-100` |
| `CciLevelSell` | Limiar vendido suave quando o preço está abaixo da MA (+100). | `100` |
| `MaPeriod` | Comprimento da LWMA. | `200` |
| `MaShift` | Deslocamento horizontal da LWMA em barras. O candle atual é comparado com o valor `MaShift` barras atrás. | `0` |
| `CandleType` | Tipo de dados de candle/período usado para cálculos. | `1 hour time frame` |

## Detalhes de implementação
- **Cálculo de pip** – O tamanho do pip é igual a `Security.PriceStep`. Para símbolos forex de 3 ou 5 decimais é multiplicado por 10 para traduzir 0.00001 para o pip 0.0001 usado pelo EA.
- **Filtro de sessão** – Implementa os dois cenários do código MQL: janelas intradiárias (`StartHour < EndHour`) e janelas noturnas (`StartHour > EndHour`). Quando `StartHour == EndHour`, a negociação é desabilitada, correspondendo à lógica original.
- **Vinculação de indicadores** – Usa `SubscribeCandles().Bind(...)` para que o CCI e a LWMA recebam atualizações automáticas sem buffering manual. Os valores são armazenados apenas para suportar o deslocamento opcional da LWMA.
- **Gestão de ordens** – `CancelActiveOrders()` é executado antes de cada ordem de mercado, refletindo o comportamento do EA de manter um livro de ordens limpo.
- **Sem versão Python** – Apenas a estratégia C# é fornecida, conforme solicitado.

## Uso
1. Anexar a estratégia a um instrumento e definir `CandleType` para o período desejado.
2. Escolher o volume e os parâmetros de pip adequados para o instrumento (lembrar de alinhar as definições de pip do corretor com a conversão incorporada).
3. Habilitar ou desabilitar o filtro de sessão de acordo com os horários de negociação.
4. Iniciar a estratégia; ela assinará candles, aplicará a lógica do indicador e gerenciará ordens/stops automaticamente.
