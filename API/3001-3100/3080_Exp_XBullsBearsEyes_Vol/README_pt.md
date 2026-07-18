# Estratégia Exp XBullsBearsEyes Vol
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma conversão em C# do expert de MetaTrader **Exp_XBullsBearsEyes_Vol**. O consultor original combina as leituras de Bulls Power e Bears Power, multiplica o resultado pelo volume do candle e colore o histograma de acordo com o momentum resultante. Dois slots de posição independentes são mantidos tanto para o lado longo quanto para o curto, permitindo ao sistema escalar quando a intensidade da cor aumenta. O port para StockSharp recria o filtro multi-estágio, a lógica de cores e o gerenciamento de negociações enquanto usa chamadas de API de alto nível para ordens e controle de risco.

O algoritmo subscreve um período configurável, reconstrói o indicador XBullsBearsEyes personalizado e reage apenas a candles concluídos. As transições de cor determinam tanto as entradas quanto as saídas: cores de alta fecham negociações curtas e podem abrir um ou dois slots longos; cores de baixa realizam a ação espelhada. As distâncias de stop-loss e take-profit são traduzidas em parâmetros de `StartProtection` para que os gerenciadores de risco da plataforma possam lidar com ordens protetoras.

## Lógica do indicador
1. Os valores de Bulls Power e Bears Power são reconstruídos com uma EMA de período `IndicatorPeriod` usando a máxima/mínima do candle contra o fechamento suavizado.
2. Um filtro adaptativo de quatro estágios acumula pressão de alta (`CU`) e de baixa (`CD`) com coeficiente `Gamma`. O valor do indicador é `CU / (CU + CD) * 100 - 50`.
3. O valor filtrado é multiplicado pelo volume de tick ou volume real, dependendo de `VolumeType`.
4. As séries multiplicadas e o volume bruto são suavizados por uma média móvel escolhida através de `SmoothingMethod`, `SmoothingLength` e `SmoothingPhase` (a fase Jurik é respeitada quando a classe subjacente a expõe).
5. Os níveis de cor são derivados de `HighLevel1`, `HighLevel2`, `LowLevel1` e `LowLevel2`. Valores acima das bandas superiores produzem cores `0` ou `1`, enquanto valores abaixo das bandas inferiores produzem cores `3` ou `4`. A cor `2` indica um estado neutro.
6. O histórico de cores é armazenado para que os sinais possam ser avaliados na barra `SignalBar` (padrão: um candle fechado atrás). A cor da barra de sinal atual é comparada com a cor anterior para detectar transições.

## Regras de negociação
- As cores `1` e `0` denotam pressão de alta. Quando a cor muda para um desses valores e a cor anterior era mais fraca, o slot 1 (`PrimaryVolume`) ou slot 2 (`SecondaryVolume`) abre uma posição longa respectivamente. Ambos os eventos fecham qualquer exposição curta existente se `AllowShortExit` estiver habilitado.
- As cores `3` e `4` denotam pressão de baixa. Quando a cor se move para esses valores e a cor anterior era mais forte, o slot 1 ou slot 2 abre uma posição curta respectivamente. Ambos os eventos fecham qualquer exposição longa existente se `AllowLongExit` estiver habilitado.
- Cada slot lembra se já tem uma posição aberta e ignora sinais repetidos até que a direção correspondente tenha sido fechada.
- `SignalBar` define quantos candles concluídos são pulados antes de avaliar a cor (0 = último candle terminado). O código requer pelo menos dois históricos de cores para comparar.
- Stop-loss e take-profit expressos em pontos (`StopLossPoints`, `TakeProfitPoints`) são convertidos em distâncias de preço absoluto com `Security.PriceStep` e usados para iniciar a proteção da plataforma com saídas de mercado.

## Parâmetros
| Nome | Descrição |
|------|-----------|
| `PrimaryVolume` | Volume para o primeiro slot (acionado pela cor 1 / 3). |
| `SecondaryVolume` | Volume para o segundo slot (acionado pela cor 0 / 4). |
| `StopLossPoints` / `TakeProfitPoints` | Distâncias protetoras em passos de preço. Definir como zero para desabilitar. |
| `AllowLongEntry` / `AllowShortEntry` | Habilitar escalonamento na direção correspondente. |
| `AllowLongExit` / `AllowShortExit` | Habilitar saídas automatizadas quando a cor oposta aparecer. |
| `CandleType` | Período subscrito para candles e cálculo do indicador (padrão: 8 horas). |
| `IndicatorPeriod` | Período EMA usado para reconstruir Bulls/Bears Power. |
| `Gamma` | Fator de suavização adaptativo para o filtro de quatro estágios (0.0 – 0.999). |
| `VolumeType` | Selecionar volume de tick ou volume real para ponderação. |
| `HighLevel1`, `HighLevel2`, `LowLevel1`, `LowLevel2` | Multiplicadores de nível que definem os limiares de cor. |
| `SmoothingMethod` | Tipo de média móvel usado para suavizar o indicador e o volume (SMA, EMA, SMMA, LWMA, Jurik, JurX, ParMA→EMA, T3, VIDYA→EMA, AMA). |
| `SmoothingLength` | Comprimento da média móvel de suavização. |
| `SmoothingPhase` | Parâmetro de fase Jurik (limitado a [-100, 100]). |
| `SignalBar` | Número de candles fechados a recuar antes de avaliar as transições de cor. |

## Notas de uso
- A estratégia opera com um único instrumento retornado por `GetWorkingSecurities()` e usa ordens de mercado para entradas e saídas.
- O gerenciamento de slots é líquido: entradas adicionais somam à posição líquida, enquanto saídas aplainam toda a exposição para o lado afetado.
- Se a plataforma fornecer apenas volume de tick, selecionar `VolumeType = Real` recorrerá à contagem de tick disponível.
- As suavizações VIDYA e Parabólica recorrem a médias móveis exponenciais porque o StockSharp expõe essas implementações diretamente.
- Certificar-se de configurar o passo de preço do instrumento para que `StopLossPoints` e `TakeProfitPoints` sejam convertidos nas distâncias absolutas pretendidas.
