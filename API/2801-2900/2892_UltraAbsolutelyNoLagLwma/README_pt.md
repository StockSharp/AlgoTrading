# Estratégia UltraAbsolutelyNoLag LWMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral

A **Estratégia UltraAbsolutelyNoLag LWMA** replica os sinais do especialista MetaTrader Ultra Absolutely No Lag LWMA usando a API de alto nível do StockSharp. A pilha de indicadores avalia uma escada de média móvel ponderada dupla e mede quantas etapas de suavização apontam para cima ou para baixo. As contagens resultantes são suavizadas novamente para gerar um estado codificado por cores que impulsiona a lógica de trading. A estratégia opcionalmente coloca ordens de proteção de stop-loss e take-profit para cada nova posição.

## Pipeline do Indicador

1. **Filtro LWMA duplo** – o preço aplicado (fechamento por padrão) é processado por duas médias móveis ponderadas consecutivas para remover ruído.
2. **Escada de suavização** – a série filtrada passa por um conjunto configurável de médias móveis. Cada passo usa o método de suavização selecionado (Jurik por padrão) e um comprimento que aumenta com um passo fixo.
3. **Contador alta/baixa** – cada passo compara o valor atual com o valor anterior. Passos em alta contribuem para o contador de alta, passos em baixa para o contador de baixa.
4. **Suavização final** – os contadores de alta e baixa são suavizados novamente usando o método selecionado. Esses dois valores formam o estado final do indicador.

A estratégia recria a lógica de cores do indicador original: estados fortemente de alta produzem códigos 7–8, estados moderadamente de alta 5–6, estados fortemente de baixa 1–2 e estados moderadamente de baixa 3–4. Zero denota um estado indefinido.

## Lógica de Trading

* Quando a barra mais antiga reportou um código de alta (`> 4`) e a barra mais recente muda para um código de baixa (`< 5` e diferente de zero), a estratégia fecha posições vendidas abertas e pode abrir uma nova posição comprada.
* Quando a barra mais antiga reportou um código de baixa (`< 5` e diferente de zero) e a barra mais recente muda para um código de alta (`> 4`), a estratégia fecha posições compradas abertas e pode abrir uma nova posição vendida.
* Ordens de stop-loss e take-profit podem ser registradas automaticamente após cada entrada quando os offsets correspondentes são maiores que zero.

A avaliação usa as duas barras completadas anteriores do período do indicador, correspondendo ao comportamento do especialista MetaTrader que trabalha no fechamento da barra.

## Parâmetros

| Nome | Descrição |
| ---- | --------- |
| `CandleType` | Tipo/período de vela usado para os cálculos do indicador. |
| `BaseLength` | Comprimento do pré-filtro LWMA duplo. |
| `AppliedPriceMode` | Preço aplicado (fechamento, abertura, típico, DeMark, etc.) usado como entrada do indicador. |
| `TrendMethod` | Método de média móvel para a escada de suavização (Jurik, SMA, EMA, etc.). |
| `StartLength` | Comprimento inicial da escada de suavização. |
| `StepSize` | Passo adicionado ao comprimento de suavização em cada estágio da escada. |
| `StepsTotal` | Número de estágios na escada de suavização. |
| `SmoothingMethod` | Método usado para suavizar os contadores de alta/baixa. |
| `SmoothingLength` | Comprimento do estágio de suavização final. |
| `UpLevelPercent` | Limiar de porcentagem que marca um estado fortemente de alta. |
| `DownLevelPercent` | Limiar de porcentagem que marca um estado fortemente de baixa. |
| `SignalBar` | Índice da barra usada para sinais de trading (1 = barra fechada anterior). |
| `AllowBuyOpen` / `AllowSellOpen` | Habilitar abertura de posições compradas/vendidas. |
| `AllowBuyClose` / `AllowSellClose` | Habilitar fechamento de posições compradas/vendidas existentes. |
| `StopLossOffset` | Distância absoluta entre o preço de entrada e o stop-loss protetor (0 desabilita). |
| `TakeProfitOffset` | Distância absoluta entre o preço de entrada e o take-profit (0 desabilita). |

## Notas de Uso

1. Configurar o tipo de vela para corresponder ao período do indicador desejado (a versão MetaTrader usa H4 por padrão).
2. Ajustar os parâmetros da escada se reações mais rápidas ou lentas forem necessárias. Um `StepsTotal` maior cria um indicador mais suave mas mais lento.
3. Deixar `StopLossOffset` e `TakeProfitOffset` em zero para desabilitar ordens protetoras.
4. O mapeamento do indicador usa médias móveis do StockSharp. Métodos não disponíveis no StockSharp recorrem à suavização Jurik ou EMA.
5. A estratégia só opera em velas terminadas para permanecer consistente com o especialista original.
